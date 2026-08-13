namespace CodexTray.Core;

public sealed class SessionRefreshGate
{
    private int _paused;

    public bool IsPaused => Volatile.Read(ref _paused) == 1;

    public bool Pause() => Interlocked.Exchange(ref _paused, 1) == 0;

    public bool Resume() => Interlocked.Exchange(ref _paused, 0) == 1;
}
