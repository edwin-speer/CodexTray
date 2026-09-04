using System.Text.Json;

namespace CodexTray.Core;

public enum CodexActivity
{
    Done,
    Busy,
    Waiting
}

public static class CodexActivityStore
{
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodexTray",
        "activity.json");

    public static CodexActivity Read()
    {
        try
        {
            var states = ReadStates().Values
                .Where(entry => entry.UpdatedAt > DateTimeOffset.UtcNow.AddDays(-1))
                .Select(entry => entry.State)
                .ToArray();
            return states.Contains(CodexActivity.Waiting) ? CodexActivity.Waiting
                : states.Contains(CodexActivity.Busy) ? CodexActivity.Busy
                : CodexActivity.Done;
        }
        catch
        {
            return CodexActivity.Done;
        }
    }

    public static void UpdateFromHook(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var sessionId = root.GetProperty("session_id").GetString();
            var eventName = root.GetProperty("hook_event_name").GetString();
            var message = root.TryGetProperty("last_assistant_message", out var messageProperty)
                ? messageProperty.GetString()
                : null;
            var activity = ClassifyHook(eventName, message);
            if (string.IsNullOrWhiteSpace(sessionId) || activity is null)
            {
                return;
            }

            using var mutex = new Mutex(false, @"Local\CodexTray.ActivityState");
            if (!mutex.WaitOne(TimeSpan.FromSeconds(1)))
            {
                return;
            }

            try
            {
                var states = ReadStates();
                states[sessionId] = new ActivityEntry(activity.Value, DateTimeOffset.UtcNow);
                Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
                File.WriteAllText(StatePath, JsonSerializer.Serialize(states));
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        catch
        {
            // Status reporting must never interrupt Codex.
        }
    }

    public static CodexActivity? ClassifyHook(string? eventName, string? lastAssistantMessage) => eventName switch
    {
        "UserPromptSubmit" => CodexActivity.Busy,
        // ponytail: question-mark detection covers normal Codex questions; use app-server events if its desktop pipe becomes public.
        "Stop" => lastAssistantMessage?.TrimEnd().EndsWith('?') == true
            ? CodexActivity.Waiting
            : CodexActivity.Done,
        "Interrupt" or "SessionEnd" => CodexActivity.Done,
        _ => null
    };

    private static Dictionary<string, ActivityEntry> ReadStates()
    {
        if (!File.Exists(StatePath))
        {
            return [];
        }

        return JsonSerializer.Deserialize<Dictionary<string, ActivityEntry>>(File.ReadAllText(StatePath)) ?? [];
    }

    private sealed record ActivityEntry(CodexActivity State, DateTimeOffset UpdatedAt);
}
