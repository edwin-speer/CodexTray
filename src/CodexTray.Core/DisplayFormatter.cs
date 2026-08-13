using System.Globalization;

namespace CodexTray.Core;

public static class DisplayFormatter
{
    public static string WindowLine(string label, LimitWindow? window, DateTimeOffset now)
    {
        if (window is null)
        {
            return $"{label}: unavailable";
        }

        var remaining = Math.Round(window.RemainingPercent);
        var used = Math.Round(window.UsedPercent);
        var reset = window.ResetsAt is { } resetsAt
            ? $" · resets {Countdown(resetsAt, now)}"
            : string.Empty;
        return $"{label}: {remaining:0}% left ({used:0}% used){reset}";
    }

    public static string BuildTooltip(CodexSnapshot snapshot)
    {
        var tooltip = snapshot.SessionWindow is { } sessionWindow
            ? $"Codex: {Math.Round(sessionWindow.RemainingPercent):0}% left"
              + (snapshot.WeeklyWindow is { } weeklyWindow
                  ? $", week {Math.Round(weeklyWindow.RemainingPercent):0}%"
                  : string.Empty)
            : snapshot.WeeklyWindow is { } weeklyOnly
                ? $"Codex: week {Math.Round(weeklyOnly.RemainingPercent):0}% left"
                : "Codex: unavailable";
        return tooltip.Length <= 63 ? tooltip : tooltip[..63];
    }

    public static string CompactNumber(long value)
    {
        return value switch
        {
            >= 1_000_000_000 => $"{value / 1_000_000_000d:0.#}B",
            >= 1_000_000 => $"{value / 1_000_000d:0.#}M",
            >= 1_000 => $"{value / 1_000d:0.#}K",
            _ => value.ToString(CultureInfo.InvariantCulture)
        };
    }

    private static string Countdown(DateTimeOffset target, DateTimeOffset now)
    {
        var remaining = target - now;
        if (remaining <= TimeSpan.Zero)
        {
            return "now";
        }

        if (remaining.TotalDays >= 2)
        {
            return $"in {(int)remaining.TotalDays}d {remaining.Hours}h";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"in {(int)remaining.TotalHours}h {remaining.Minutes}m";
        }

        return $"in {Math.Max(1, remaining.Minutes)}m";
    }
}
