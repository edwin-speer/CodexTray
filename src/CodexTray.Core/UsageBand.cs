namespace CodexTray.Core;

public enum UsageBand
{
    Unknown,
    Green,
    Amber,
    Red
}

public static class UsageBandSelector
{
    public static UsageBand Select(double? remainingPercent) => remainingPercent switch
    {
        null => UsageBand.Unknown,
        > 50d => UsageBand.Green,
        > 20d => UsageBand.Amber,
        _ => UsageBand.Red
    };
}
