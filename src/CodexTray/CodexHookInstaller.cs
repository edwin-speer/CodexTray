using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using CodexTray.Core;

namespace CodexTray;

internal static class CodexHookInstaller
{
    public static bool TryInstall()
    {
        try
        {
            var codexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
            if (string.IsNullOrWhiteSpace(codexHome))
            {
                codexHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
            }

            var hooksPath = Path.Combine(codexHome, "hooks.json");
            var root = File.Exists(hooksPath)
                ? JsonNode.Parse(File.ReadAllText(hooksPath)) as JsonObject
                : new JsonObject { ["description"] = "Publish Codex task activity to CodexTray." };
            if (root is null)
            {
                return false;
            }

            var assembly = Path.Combine(AppContext.BaseDirectory, "CodexTray.dll");
            var command = $"dotnet \"{assembly}\" --status-hook";
            if (!CodexHookConfiguration.EnsureInstalled(root, command))
            {
                return false;
            }

            Directory.CreateDirectory(codexHome);
            var json = root.ToJsonString(new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            });
            var temporaryPath = hooksPath + ".codextray.tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, hooksPath, overwrite: true);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
