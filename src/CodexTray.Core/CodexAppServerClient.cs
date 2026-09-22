using System.Diagnostics;
using System.Text.Json;

namespace CodexTray.Core;

public sealed class CodexAppServerClient
{
    private readonly string _codexPath;
    private readonly TimeSpan _timeout;

    public CodexAppServerClient(string codexPath, TimeSpan? timeout = null)
    {
        _codexPath = Path.GetFullPath(codexPath);
        _timeout = timeout ?? TimeSpan.FromSeconds(20);
    }

    public async Task<CodexSnapshot> FetchAsync(CancellationToken cancellationToken = default)
    {
        return await RunAsync(async (writer, reader, token) =>
        {
            await InitializeAsync(writer, reader, token).ConfigureAwait(false);
            await SendAsync(writer, new
            {
                method = "account/read",
                id = 2,
                @params = new { refreshToken = false }
            }, token).ConfigureAwait(false);
            await SendAsync(writer, new
            {
                method = "account/rateLimits/read",
                id = 3,
                @params = new { }
            }, token).ConfigureAwait(false);
            await SendAsync(writer, new
            {
                method = "account/usage/read",
                id = 4,
                @params = new { }
            }, token).ConfigureAwait(false);

            var responses = await ReadResponsesAsync(reader, [2, 3, 4], token).ConfigureAwait(false);
            return CodexSnapshotParser.Parse(
                responses.GetValueOrDefault(2),
                responses[3],
                responses.GetValueOrDefault(4),
                DateTimeOffset.Now);
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> ConsumeResetCreditAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await RunAsync(async (writer, reader, token) =>
        {
            await InitializeAsync(writer, reader, token).ConfigureAwait(false);
            await SendAsync(writer, new
            {
                method = "account/rateLimitResetCredit/consume",
                id = 2,
                @params = new { idempotencyKey }
            }, token).ConfigureAwait(false);
            var responses = await ReadResponsesAsync(reader, [2], token).ConfigureAwait(false);
            return CodexSnapshotParser.ParseResetCreditOutcome(responses[2]);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static async Task InitializeAsync(
        StreamWriter writer,
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        await SendAsync(writer, new
        {
            method = "initialize",
            id = 1,
            @params = new
            {
                clientInfo = new
                {
                    name = "CodexTray",
                    title = "Codex Tray",
                    version = typeof(CodexAppServerClient).Assembly.GetName().Version?.ToString(3) ?? "unknown"
                }
            }
        }, cancellationToken).ConfigureAwait(false);
        await ReadResponsesAsync(reader, [1], cancellationToken).ConfigureAwait(false);
        await SendAsync(writer, new { method = "initialized", @params = new { } }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T> RunAsync<T>(
        Func<StreamWriter, StreamReader, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        using var process = StartProcess();
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            return await action(process.StandardInput, process.StandardOutput, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Codex app-server did not answer within {_timeout.TotalSeconds:0} seconds.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var stderr = await ReadStderrAfterStopAsync(process, stderrTask).ConfigureAwait(false);
            var detail = FirstUsefulLine(stderr);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(detail) ? ex.Message : $"{ex.Message} ({detail})",
                ex);
        }
        finally
        {
            if (TryStop(process))
            {
                try
                {
                    await process.WaitForExitAsync(CancellationToken.None)
                        .WaitAsync(TimeSpan.FromSeconds(2))
                        .ConfigureAwait(false);
                }
                catch
                {
                    // Best-effort child cleanup.
                }
            }
        }
    }

    private Process StartProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _codexPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = AppContext.BaseDirectory
        };
        startInfo.ArgumentList.Add("app-server");
        startInfo.ArgumentList.Add("--listen");
        startInfo.ArgumentList.Add("stdio://");
        return Process.Start(startInfo)
               ?? throw new InvalidOperationException("Could not start the Codex app-server.");
    }

    private static async Task SendAsync(
        StreamWriter writer,
        object message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await writer.WriteLineAsync(JsonSerializer.Serialize(message)).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Dictionary<int, JsonElement>> ReadResponsesAsync(
        StreamReader reader,
        IReadOnlyCollection<int> expectedIds,
        CancellationToken cancellationToken)
    {
        var responses = new Dictionary<int, JsonElement>();
        while (expectedIds.Any(id => !responses.ContainsKey(id)))
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                throw new InvalidOperationException("Codex app-server closed its output unexpectedly.");
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;
                if (root.TryGetProperty("id", out var idProperty)
                    && idProperty.TryGetInt32(out var id)
                    && expectedIds.Contains(id))
                {
                    responses[id] = root.Clone();
                }
            }
            catch (JsonException)
            {
                // Ignore non-protocol noise and keep waiting for the requested ids.
            }
        }

        return responses;
    }

    private static async Task<string> ReadStderrAfterStopAsync(Process process, Task<string> stderrTask)
    {
        TryStop(process);
        try
        {
            return await stderrTask.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FirstUsefulLine(string value)
    {
        return value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => !line.Contains("plugin", StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
    }

    private static bool TryStop(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return true;
            }

            process.Kill(entireProcessTree: true);
            return true;
        }
        catch
        {
            // Best-effort child cleanup.
            return false;
        }
    }
}
