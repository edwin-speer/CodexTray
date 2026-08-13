using CodexTray.Core;

namespace CodexTray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _accountItem = DisabledItem("Codex account");
    private readonly ToolStripMenuItem _sessionItem = DisabledItem("Short window: loading...");
    private readonly ToolStripMenuItem _weeklyItem = DisabledItem("Weekly: loading...");
    private readonly ToolStripMenuItem _creditsItem = DisabledItem("Reset credits: loading...");
    private readonly ToolStripMenuItem _usageItem = DisabledItem("Token usage: loading...");
    private readonly ToolStripMenuItem _updatedItem = DisabledItem("Not refreshed yet");
    private readonly ToolStripMenuItem _refreshItem = new("Refresh now");
    private readonly ToolStripMenuItem _startupItem = new("Start with Windows") { CheckOnClick = false };
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly System.Windows.Forms.Timer _initialTimer;
    private readonly NotificationStateStore _notificationStateStore = new();
    private CodexAppServerClient? _client;
    private CodexSnapshot? _lastSnapshot;
    private NotificationState? _notificationState;
    private Icon? _currentIcon;
    private bool _refreshing;
    private bool _shownFailure;

    public TrayApplicationContext()
    {
        _notificationState = _notificationStateStore.Load();
        _menu.Items.AddRange([
            _accountItem,
            new ToolStripSeparator(),
            _sessionItem,
            _weeklyItem,
            _creditsItem,
            _usageItem,
            new ToolStripSeparator(),
            _updatedItem,
            _refreshItem,
            _startupItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit", null, (_, _) => ExitThread())
        ]);
        _menu.Opening += (_, _) => SyncStartupMenu();
        _refreshItem.Click += async (_, _) => await RefreshAsync(showFailureBalloon: true);
        _startupItem.Click += (_, _) => ToggleStartup();

        _currentIcon = TrayIconRenderer.Render(null);
        _notifyIcon = new NotifyIcon
        {
            Icon = _currentIcon,
            Text = "Codex: loading usage",
            Visible = true,
            ContextMenuStrip = _menu
        };
        _notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
            {
                _menu.Show(Cursor.Position);
            }
        };

        _refreshTimer = new System.Windows.Forms.Timer { Interval = (int)RefreshInterval.TotalMilliseconds };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync(showFailureBalloon: false);
        _refreshTimer.Start();

        _initialTimer = new System.Windows.Forms.Timer { Interval = 150 };
        _initialTimer.Tick += async (_, _) =>
        {
            _initialTimer.Stop();
            await RefreshAsync(showFailureBalloon: true);
        };
        _initialTimer.Start();
    }

    protected override void ExitThreadCore()
    {
        _initialTimer.Stop();
        _refreshTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _currentIcon?.Dispose();
        _menu.Dispose();
        base.ExitThreadCore();
    }

    private async Task RefreshAsync(bool showFailureBalloon)
    {
        if (_refreshing)
        {
            return;
        }

        _refreshing = true;
        _refreshItem.Enabled = false;
        _updatedItem.Text = "Refreshing...";
        try
        {
            _client ??= new CodexAppServerClient(CodexLocator.Locate());
            var snapshot = await _client.FetchAsync();
            var notifications = UsageNotificationDetector.Detect(_notificationState, snapshot);
            _lastSnapshot = snapshot;
            _notificationState = NotificationState.FromSnapshot(snapshot);
            _notificationStateStore.Save(_notificationState);
            _shownFailure = false;
            ApplySnapshot(snapshot);
            ShowUsageNotifications(notifications);
        }
        catch (Exception ex)
        {
            ApplyError(ex.Message);
            if (showFailureBalloon && !_shownFailure)
            {
                _shownFailure = true;
                _notifyIcon.BalloonTipTitle = "Codex Tray could not refresh";
                _notifyIcon.BalloonTipText = Shorten(ex.Message, 220);
                _notifyIcon.ShowBalloonTip(5000);
            }
        }
        finally
        {
            _refreshing = false;
            _refreshItem.Enabled = true;
        }
    }

    private void ApplySnapshot(CodexSnapshot snapshot)
    {
        var account = snapshot.AccountDisplay ?? "Signed-in Codex account";
        _accountItem.Text = string.IsNullOrWhiteSpace(snapshot.PlanType)
            ? account
            : $"{account} · {snapshot.PlanType}";
        _sessionItem.Visible = snapshot.SessionWindow is not null;
        _sessionItem.Text = DisplayFormatter.WindowLine("Short window", snapshot.SessionWindow, DateTimeOffset.Now);
        _weeklyItem.Text = DisplayFormatter.WindowLine("Weekly", snapshot.WeeklyWindow, DateTimeOffset.Now);
        _creditsItem.Text = snapshot.AvailableResetCredits is { } credits
            ? $"Reset credits: {credits}"
            : "Reset credits: not reported";
        _usageItem.Text = UsageLine(snapshot.Usage);
        _updatedItem.Text = $"Updated {snapshot.FetchedAt.LocalDateTime:t}";
        _notifyIcon.Text = DisplayFormatter.BuildTooltip(snapshot);
        ReplaceIcon(snapshot.DisplayWindow?.RemainingPercent);
    }

    private void ApplyError(string message)
    {
        _updatedItem.Text = $"Refresh failed: {Shorten(message, 90)}";
        _notifyIcon.Text = _lastSnapshot is null
            ? "Codex: refresh failed"
            : DisplayFormatter.BuildTooltip(_lastSnapshot) + " (stale)";
        if (_lastSnapshot is null)
        {
            ReplaceIcon(null);
        }
    }

    private void ReplaceIcon(double? remainingPercent)
    {
        var next = TrayIconRenderer.Render(remainingPercent);
        _notifyIcon.Icon = next;
        var previous = _currentIcon;
        _currentIcon = next;
        previous?.Dispose();
    }

    private void ShowUsageNotifications(IReadOnlyList<UsageNotification> notifications)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = notifications.Count == 1
            ? notifications[0].Title
            : "Codex usage updated";
        _notifyIcon.BalloonTipText = string.Join(Environment.NewLine, notifications.Select(item => item.Message));
        _notifyIcon.ShowBalloonTip(8000);
    }

    private static string UsageLine(UsageSummary? usage)
    {
        if (usage is null)
        {
            return "Token usage: not reported";
        }

        var parts = new List<string>();
        if (usage.TodayTokens is { } today)
        {
            parts.Add($"today {DisplayFormatter.CompactNumber(today)}");
        }
        if (usage.LifetimeTokens is { } lifetime)
        {
            parts.Add($"lifetime {DisplayFormatter.CompactNumber(lifetime)}");
        }
        return parts.Count == 0 ? "Token usage: not reported" : $"Tokens: {string.Join(" · ", parts)}";
    }

    private void SyncStartupMenu()
    {
        try
        {
            _startupItem.Checked = StartupRegistration.IsEnabled();
        }
        catch
        {
            _startupItem.Checked = false;
        }
    }

    private void ToggleStartup()
    {
        try
        {
            StartupRegistration.SetEnabled(!StartupRegistration.IsEnabled());
            SyncStartupMenu();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Codex Tray",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static ToolStripMenuItem DisabledItem(string text) => new(text) { Enabled = false };

    private static string Shorten(string value, int length) =>
        value.Length <= length ? value : value[..Math.Max(0, length - 3)] + "...";
}
