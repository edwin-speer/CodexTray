namespace CodexTray.Core;

public static class DisplayFormatter
{
    public static string WindowLine(string label, LimitWindow? window, DateTimeOffset now)
    {
        if (window is null)
        {
            return $"{label}: unavailable";
        }

        var reset = window.ResetsAt is { } resetsAt
            ? $" · resets {Countdown(resetsAt, now)}"
            : string.Empty;
        return $"{label}{reset}";
    }

    public static string CreditsLine(int? availableResetCredits) => availableResetCredits is { } credits
        ? $"Reset credits: {credits}"
        : "Reset credits: not reported";

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
