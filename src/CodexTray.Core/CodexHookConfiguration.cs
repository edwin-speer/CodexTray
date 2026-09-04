using System.Text.Json;
using System.Text.Json.Nodes;

namespace CodexTray.Core;

public static class CodexHookConfiguration
{
    private static readonly string[] Events = ["UserPromptSubmit", "Stop", "Interrupt", "SessionEnd"];

    public static bool EnsureInstalled(JsonObject root, string command)
    {
        var changed = false;
        if (root["hooks"] is not null and not JsonObject)
        {
            throw new JsonException("The hooks property must be an object.");
        }

        var hooks = root["hooks"] as JsonObject;
        if (hooks is null)
        {
            hooks = new JsonObject();
            root["hooks"] = hooks;
            changed = true;
        }

        foreach (var eventName in Events)
        {
            if (hooks[eventName] is not null and not JsonArray)
            {
                throw new JsonException($"The {eventName} hooks must be an array.");
            }

            var groups = hooks[eventName] as JsonArray;
            if (groups is null)
            {
                groups = [];
                hooks[eventName] = groups;
                changed = true;
            }

            var matches = groups
                .OfType<JsonObject>()
                .Select(group => group["hooks"] as JsonArray)
                .Where(handlers => handlers is not null)
                .SelectMany(handlers => handlers!.OfType<JsonObject>().Select(handler => (handlers, handler)))
                .Where(item => IsCodexTrayHook(item.handler))
                .ToList();

            if (matches.Count == 0)
            {
                groups.Add(NewGroup(command));
                changed = true;
                continue;
            }

            changed |= SetCanonical(matches[0].handler, command);
            foreach (var duplicate in matches.Skip(1))
            {
                duplicate.handlers!.Remove(duplicate.handler);
                changed = true;
            }
        }

        return changed;
    }

    private static bool IsCodexTrayHook(JsonObject handler)
    {
        var command = StringValue(handler["command"]) ?? StringValue(handler["commandWindows"]);
        return command?.Contains("CodexTray", StringComparison.OrdinalIgnoreCase) == true
               && command.Contains("--status-hook", StringComparison.OrdinalIgnoreCase);
    }

    private static JsonObject NewGroup(string command) => new()
    {
        ["hooks"] = new JsonArray(NewHandler(command))
    };

    private static JsonObject NewHandler(string command) => new()
    {
        ["type"] = "command",
        ["command"] = command,
        ["timeout"] = 3
    };

    private static bool SetCanonical(JsonObject handler, string command)
    {
        var changed = StringValue(handler["type"]) != "command"
                      || StringValue(handler["command"]) != command
                      || IntValue(handler["timeout"]) != 3
                      || handler.ContainsKey("commandWindows");
        handler["type"] = "command";
        handler["command"] = command;
        handler["timeout"] = 3;
        handler.Remove("commandWindows");
        return changed;
    }

    private static string? StringValue(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var result) ? result : null;

    private static int? IntValue(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<int>(out var result) ? result : null;
}
