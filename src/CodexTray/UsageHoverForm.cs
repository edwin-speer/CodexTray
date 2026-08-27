using CodexTray.Core;
using System.Runtime.InteropServices;

namespace CodexTray;

internal sealed class UsageHoverForm : Form
{
    private readonly Label _session = DetailLabel();
    private readonly ProgressBar _sessionBar = UsageBar("Daily usage");
    private readonly Label _weekly = DetailLabel();
    private readonly ProgressBar _weeklyBar = UsageBar("Weekly usage");
    private readonly Label _credits = DetailLabel();
    private readonly Label _usage = DetailLabel();
    private readonly Label _updated = DetailLabel(Color.FromArgb(148, 163, 184));
    private readonly Button _pinButton = ActionButton("📌", "Pin window");
    private readonly Button _closeButton = ActionButton("✕", "Close window");

    public event EventHandler? PinnedChanged;

    public bool IsPinned { get; private set; }

    public UsageHoverForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.FromArgb(30, 41, 59);
        ForeColor = Color.FromArgb(241, 245, 249);
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        Padding = new Padding(1);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Text = "Codex Tray usage";
        TopMost = true;

        _pinButton.Click += (_, _) => SetPinned(true);
        _closeButton.Click += (_, _) =>
        {
            SetPinned(false);
            Hide();
        };
        _closeButton.Visible = false;

        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            ColumnCount = 1,
            Padding = new Padding(13, 11, 18, 12)
        };
        var actions = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 0, 0, 4),
            WrapContents = false
        };
        actions.Controls.Add(_pinButton);
        actions.Controls.Add(_closeButton);
        panel.Controls.Add(actions);
        panel.Controls.Add(_session);
        panel.Controls.Add(_sessionBar);
        panel.Controls.Add(_weekly);
        panel.Controls.Add(_weeklyBar);
        panel.Controls.Add(_credits);
        panel.Controls.Add(_usage);
        panel.Controls.Add(_updated);
        Controls.Add(panel);
        EnableDragging(panel);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int wsExToolWindow = 0x00000080;
            var parameters = base.CreateParams;
            parameters.ExStyle |= wsExToolWindow;
            return parameters;
        }
    }

    public bool ContainsCursor => Bounds.Contains(Cursor.Position);

    public void UpdateSnapshot(CodexSnapshot snapshot)
    {
        var now = DateTimeOffset.Now;
        SetWindow(_session, _sessionBar, "Daily", snapshot.SessionWindow, now);
        SetWindow(_weekly, _weeklyBar, "Weekly", snapshot.WeeklyWindow, now);
        _credits.Text = DisplayFormatter.CreditsLine(snapshot.AvailableResetCredits);
        _usage.Text = DisplayFormatter.UsageLine(snapshot.Usage);
        _updated.Text = $"Updated {snapshot.FetchedAt.LocalDateTime:t}";
    }

    public void ShowNear(Point anchor)
    {
        if (IsPinned)
        {
            return;
        }

        if (!Visible)
        {
            Show();
        }

        var workArea = Screen.FromPoint(anchor).WorkingArea;
        var x = Math.Clamp(anchor.X - Width + 18, workArea.Left, Math.Max(workArea.Left, workArea.Right - Width));
        var y = anchor.Y - Height - 14;
        if (y < workArea.Top)
        {
            y = Math.Min(workArea.Bottom - Height, anchor.Y + 18);
        }
        Location = new Point(x, Math.Max(workArea.Top, y));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(Color.FromArgb(71, 85, 105));
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }

    private static Label DetailLabel(Color? color = null) => new()
    {
        AutoSize = true,
        ForeColor = color ?? Color.FromArgb(226, 232, 240),
        Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
        Margin = new Padding(0, 0, 0, 4),
        MaximumSize = new Size(460, 0)
    };

    private static ProgressBar UsageBar(string accessibleName) => new()
    {
        AccessibleName = accessibleName,
        Height = 12,
        Margin = new Padding(0, 0, 0, 7),
        Maximum = 100,
        Style = ProgressBarStyle.Continuous,
        Width = 320
    };

    private static Button ActionButton(string text, string accessibleName) => new()
    {
        AccessibleName = accessibleName,
        AutoSize = true,
        Margin = Padding.Empty,
        Text = text
    };

    private void SetPinned(bool pinned)
    {
        if (IsPinned == pinned)
        {
            return;
        }

        IsPinned = pinned;
        _pinButton.Visible = !pinned;
        _closeButton.Visible = pinned;
        PinnedChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnableDragging(Control control)
    {
        if (control is not Button)
        {
            control.MouseDown += BeginDrag;
        }

        foreach (Control child in control.Controls)
        {
            EnableDragging(child);
        }
    }

    private void BeginDrag(object? sender, MouseEventArgs args)
    {
        if (IsPinned && args.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, 0x00A1, 2, 0);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint window, int message, nint parameter, nint data);

    private static void SetWindow(
        Label label,
        ProgressBar bar,
        string name,
        LimitWindow? window,
        DateTimeOffset now)
    {
        label.Visible = bar.Visible = window is not null;
        if (window is null)
        {
            return;
        }

        label.Text = DisplayFormatter.WindowLine(name, window, now, showPercentages: false);
        bar.Value = (int)Math.Round(Math.Clamp(window.UsedPercent, 0d, 100d));
    }
}
