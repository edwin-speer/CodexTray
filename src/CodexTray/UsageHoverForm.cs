using CodexTray.Core;
using Microsoft.Win32;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace CodexTray;

internal sealed class UsageHoverForm : Form
{
    private readonly Label _session = DetailLabel();
    private readonly UsageRing _sessionRing = new("Daily usage");
    private readonly Label _weekly = DetailLabel();
    private readonly UsageRing _weeklyRing = new("Weekly usage");
    private readonly Label _credits = DetailLabel();
    private readonly Button _pinButton = ActionButton("📌", "Pin window");
    private readonly Button _closeButton = ActionButton("✕", "Close window");
    private Color _borderColor;

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
        var gauges = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
            WrapContents = false
        };
        gauges.Controls.Add(Gauge(_sessionRing, _session));
        gauges.Controls.Add(Gauge(_weeklyRing, _weekly));
        panel.Controls.Add(gauges);
        panel.Controls.Add(_credits);
        Controls.Add(panel);
        EnableDragging(panel);
        ApplyTheme();
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
        SetWindow(_session, _sessionRing, "Daily", snapshot.SessionWindow, now);
        SetWindow(_weekly, _weeklyRing, "Weekly", snapshot.WeeklyWindow, now);
        _credits.Text = DisplayFormatter.CreditsLine(snapshot.AvailableResetCredits);
    }

    public void ShowNear(Point anchor)
    {
        if (IsPinned)
        {
            return;
        }

        if (!Visible)
        {
            ApplyTheme();
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
        using var border = new Pen(_borderColor);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }

    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        if (message.Msg == 0x001A)
        {
            ApplyTheme();
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        const int windowCornerPreference = 33;
        var rounded = 2;
        DwmSetWindowAttribute(Handle, windowCornerPreference, ref rounded, sizeof(int));
    }

    private static Label DetailLabel(Color? color = null) => new()
    {
        AutoSize = true,
        ForeColor = color ?? Color.FromArgb(226, 232, 240),
        Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point),
        Margin = new Padding(0, 0, 0, 4),
        MaximumSize = new Size(460, 0)
    };

    private static Button ActionButton(string text, string accessibleName) => new()
    {
        AccessibleName = accessibleName,
        AutoSize = false,
        FlatAppearance = { BorderSize = 0 },
        FlatStyle = FlatStyle.Flat,
        Margin = Padding.Empty,
        Text = text,
        Width = 24
    };

    private static Control Gauge(UsageRing ring, Label label)
    {
        label.AutoSize = false;
        label.Size = new Size(145, 38);
        label.TextAlign = ContentAlignment.TopCenter;

        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 10, 4)
        };
        ring.Anchor = AnchorStyles.None;
        panel.Controls.Add(ring);
        panel.Controls.Add(label);
        return panel;
    }

    private void ApplyTheme()
    {
        var light = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme",
            1) is not 0;
        var background = light ? Color.White : Color.Black;
        var foreground = light ? Color.Black : Color.White;

        ApplyColors(this, background, foreground);
        _borderColor = light ? Color.LightGray : Color.DimGray;
        Invalidate(true);
    }

    private static void ApplyColors(Control control, Color background, Color foreground)
    {
        control.BackColor = background;
        control.ForeColor = foreground;

        foreach (Control child in control.Controls)
        {
            ApplyColors(child, background, foreground);
        }
    }

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

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    private static void SetWindow(
        Label label,
        UsageRing ring,
        string name,
        LimitWindow? window,
        DateTimeOffset now)
    {
        label.Visible = ring.Visible = window is not null;
        if (window is null)
        {
            return;
        }

        label.Text = DisplayFormatter.WindowLine(name, window, now);
        ring.Value = (int)Math.Round(Math.Clamp(window.UsedPercent, 0d, 100d));
    }
}

internal sealed class UsageRing : Control
{
    private int _value;

    public UsageRing(string accessibleName)
    {
        AccessibleName = accessibleName;
        AccessibleRole = AccessibleRole.ProgressBar;
        DoubleBuffered = true;
        Font = new Font("Segoe UI", 14f, FontStyle.Bold, GraphicsUnit.Point);
        Margin = new Padding(0, 0, 0, 6);
        Size = new Size(76, 76);
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, 0, 100);
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(6, 6, Width - 13, Height - 13);
        var ringColor = UsageBandSelector.Select(100 - _value) switch
        {
            UsageBand.Green => Color.FromArgb(34, 197, 94),
            UsageBand.Amber => Color.FromArgb(245, 158, 11),
            UsageBand.Red => Color.FromArgb(239, 68, 68),
            _ => Color.Gray
        };
        using var tint = new SolidBrush(Color.FromArgb(28, ringColor));
        using var track = new Pen(BackColor.GetBrightness() > 0.5f ? Color.FromArgb(229, 231, 235) : Color.FromArgb(55, 65, 81), 5f);
        using var progress = new Pen(ringColor, 5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.FillEllipse(tint, bounds);
        e.Graphics.DrawEllipse(track, bounds);
        if (_value > 0)
        {
            e.Graphics.DrawArc(progress, bounds, -90f, 360f * _value / 100f);
        }
        TextRenderer.DrawText(e.Graphics, _value.ToString(), Font, ClientRectangle, ringColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
