namespace CodexTray.Core;

public sealed record UsageNotification(string Title, string Message);

public sealed record NotificationState(
    double? WeeklyUsedPercent,
    DateTimeOffset? WeeklyResetsAt,
    int? AvailableResetCredits)
{
    public static NotificationState FromSnapshot(CodexSnapshot snapshot) => new(
        snapshot.WeeklyWindow?.UsedPercent,
        snapshot.WeeklyWindow?.ResetsAt,
        snapshot.AvailableResetCredits);
}

public static class UsageNotificationDetector
{
    public static IReadOnlyList<UsageNotification> Detect(
        NotificationState? previous,
        CodexSnapshot current)
    {
        if (previous is null)
        {
            return [];
        }

        var notifications = new List<UsageNotification>();
        var currentWeekly = current.WeeklyWindow;
        if (previous.WeeklyResetsAt is { } previousReset
            && currentWeekly?.ResetsAt is { } currentReset
            && currentReset > previousReset.AddHours(12))
        {
            notifications.Add(new UsageNotification(
                "Codex weekly limit reset",
                $"Your weekly Codex window reset. {Math.Round(currentWeekly.RemainingPercent):0}% is available."));
        }

        if (previous.AvailableResetCredits is { } previousCredits
            && current.AvailableResetCredits is { } currentCredits
            && currentCredits > previousCredits)
        {
            var added = currentCredits - previousCredits;
            notifications.Add(new UsageNotification(
                "Codex reset credit added",
                $"{added} new reset credit{(added == 1 ? string.Empty : "s")} available; {currentCredits} total."));
        }

        return notifications;
    }
}
