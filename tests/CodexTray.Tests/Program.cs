using System.Text.Json;
using CodexTray.Core;

var failures = new List<string>();
Run("parses rate windows and reset credits", ParsesRateWindows, failures);
Run("parses usage summary and today's tokens", ParsesUsage, failures);
Run("prefers the codex bucket", PrefersCodexBucket, failures);
Run("formats circle labels", FormatsBarHeading, failures);
Run("detects a weekly reset", DetectsWeeklyReset, failures);
Run("detects an unused weekly reset", DetectsUnusedWeeklyReset, failures);
Run("detects a new reset credit", DetectsNewResetCredit, failures);
Run("does not duplicate a weekly-only window", DoesNotDuplicateWeeklyOnlyWindow, failures);
Run("maps tray usage colors", MapsTrayUsageColors, failures);
Run("rejects incomplete rate windows", RejectsIncompleteRateWindow, failures);

if (failures.Count > 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}

Console.WriteLine("All 10 Codex Tray checks passed.");
return 0;

static void ParsesRateWindows()
{
    var snapshot = ParseSnapshot();
    Equal(25d, snapshot.SessionWindow?.UsedPercent, "session used percent");
    Equal(75d, snapshot.SessionWindow?.RemainingPercent, "session remaining percent");
    Equal(10_080, snapshot.WeeklyWindow?.WindowMinutes, "weekly duration");
    Equal(2, snapshot.AvailableResetCredits, "reset-credit count");
    Equal("pro", snapshot.PlanType, "plan type");
}

static void ParsesUsage()
{
    var snapshot = ParseSnapshot();
    Equal(1_234_567L, snapshot.Usage?.LifetimeTokens, "lifetime tokens");
    Equal(12_345L, snapshot.Usage?.TodayTokens, "today tokens");
}

static void PrefersCodexBucket()
{
    var snapshot = ParseSnapshot();
    Equal("codex", snapshot.PreferredBucket?.Id, "preferred bucket id");
}

static void FormatsBarHeading()
{
    var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
    var window = new LimitWindow(25, 300, now.AddHours(1));
    Equal("Daily · resets in 1h 0m", DisplayFormatter.WindowLine("Daily", window, now), "circle label");
}

static void DetectsWeeklyReset()
{
    var snapshot = ParseSnapshot();
    var previous = new NotificationState(
        WeeklyResetsAt: snapshot.WeeklyWindow!.ResetsAt!.Value.AddDays(-7),
        AvailableResetCredits: snapshot.AvailableResetCredits);
    var notifications = UsageNotificationDetector.Detect(previous, snapshot);
    True(notifications.Any(item => item.Title.Contains("weekly", StringComparison.OrdinalIgnoreCase)), "weekly reset notification");
}

static void DetectsNewResetCredit()
{
    var snapshot = ParseSnapshot();
    var previous = new NotificationState(
        WeeklyResetsAt: snapshot.WeeklyWindow?.ResetsAt,
        AvailableResetCredits: 1);
    var notifications = UsageNotificationDetector.Detect(previous, snapshot);
    True(notifications.Any(item => item.Title.Contains("credit", StringComparison.OrdinalIgnoreCase)), "reset-credit notification");
}

static void DetectsUnusedWeeklyReset()
{
    var snapshot = ParseSnapshot();
    var previous = new NotificationState(
        WeeklyResetsAt: snapshot.WeeklyWindow!.ResetsAt!.Value.AddDays(-7),
        AvailableResetCredits: snapshot.AvailableResetCredits);
    var notifications = UsageNotificationDetector.Detect(previous, snapshot);
    True(notifications.Any(item => item.Title.Contains("weekly", StringComparison.OrdinalIgnoreCase)), "unused weekly reset notification");
}

static void DoesNotDuplicateWeeklyOnlyWindow()
{
    using var limits = JsonDocument.Parse("""
        {"id":3,"result":{"rateLimits":{"limitId":"codex","primary":{"usedPercent":11,"windowDurationMins":10080,"resetsAt":1894060800}}}}
        """);
    var snapshot = CodexSnapshotParser.Parse(null, limits.RootElement, null, DateTimeOffset.Now);
    Equal<LimitWindow?>(null, snapshot.SessionWindow, "weekly-only session window");
    Equal(10_080, snapshot.WeeklyWindow?.WindowMinutes, "weekly-only weekly duration");
}

static void MapsTrayUsageColors()
{
    Equal(UsageBand.Unknown, UsageBandSelector.Select(null), "unknown usage band");
    Equal(UsageBand.Green, UsageBandSelector.Select(51), "green usage band");
    Equal(UsageBand.Amber, UsageBandSelector.Select(50), "amber usage band");
    Equal(UsageBand.Amber, UsageBandSelector.Select(20), "low amber usage band");
    Equal(UsageBand.Red, UsageBandSelector.Select(19), "red usage band");
}

static void RejectsIncompleteRateWindow()
{
    using var limits = JsonDocument.Parse("""
        {"id":3,"result":{"rateLimits":{"limitId":"codex","primary":{"windowDurationMins":300}}}}
        """);
    var snapshot = CodexSnapshotParser.Parse(null, limits.RootElement, null, DateTimeOffset.Now);
    Equal<LimitWindow?>(null, snapshot.SessionWindow, "incomplete session window");
}

static CodexSnapshot ParseSnapshot()
{
    var today = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
    using var account = JsonDocument.Parse("""
        {"id":2,"result":{"account":{"type":"chatgpt","email":"user@example.com","planType":"pro"}}}
        """);
    using var limits = JsonDocument.Parse("""
        {"id":3,"result":{
          "rateLimits":{"limitId":"codex","planType":"pro","primary":{"usedPercent":25,"windowDurationMins":300,"resetsAt":1893456000},"secondary":{"usedPercent":40,"windowDurationMins":10080,"resetsAt":1894060800}},
          "rateLimitsByLimitId":{
            "other":{"limitId":"other","primary":{"usedPercent":10,"windowDurationMins":60,"resetsAt":1893456000}},
            "codex":{"limitId":"codex","planType":"pro","primary":{"usedPercent":25,"windowDurationMins":300,"resetsAt":1893456000},"secondary":{"usedPercent":40,"windowDurationMins":10080,"resetsAt":1894060800}}
          },
          "rateLimitResetCredits":{"availableCount":2,"credits":[]}
        }}
        """);
    using var usage = JsonDocument.Parse(
        "{\"id\":4,\"result\":{\"summary\":{\"lifetimeTokens\":1234567,\"peakDailyTokens\":45678,\"currentStreakDays\":8},"
        + "\"dailyUsageBuckets\":[{\"startDate\":\"" + today + "\",\"tokens\":12345}]}}");
    return CodexSnapshotParser.Parse(account.RootElement, limits.RootElement, usage.RootElement, DateTimeOffset.Now);
}

static void Run(string name, Action action, List<string> failures)
{
    try
    {
        action();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"FAIL {name}: {ex.Message}");
    }
}

static void Equal<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }
}

static void True(bool value, string label)
{
    if (!value)
    {
        throw new InvalidOperationException(label);
    }
}
