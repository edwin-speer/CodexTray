using CodexTray.Core;
using Microsoft.Win32;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace CodexTray;

internal sealed class UsageHoverForm : Form
{
    private const int GaugeLabelWidth = 145;
    private const string PreferencesKey = @"HKEY_CURRENT_USER\Software\CodexTray";
    private const string SessionResetAtPreference = "ShowSessionResetAt";
    private const string WeeklyResetAtPreference = "ShowWeeklyResetAt";
    private readonly Label _sessionTitle = HeadingLabel("Daily");
    private readonly Label _session = DetailLabel();
    private readonly UsageRing _sessionRing = new("Daily usage");
    private readonly Label _weeklyTitle = HeadingLabel("Weekly");
    private readonly Label _weekly = DetailLabel();
    private readonly UsageRing _weeklyRing = new("Weekly usage");
    private readonly Label _credits = DetailLabel();
    private readonly Panel _creditsFooter = new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private readonly Button _pinButton = ActionButton("📌", "Pin window");
    private readonly Button _closeButton = ActionButton("✕", "Close window");
    private Font? _accessibilityFont;
    private Font? _headingFont;
    private CodexSnapshot? _snapshot;
    private Color _borderColor;
    private bool _showSessionResetAt = ReadPreference(SessionResetAtPreference);
    private bool _showWeeklyResetAt = ReadPreference(WeeklyResetAtPreference);

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

        var content = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            ColumnCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(12, 11, 18, 0)
        };
        var actions = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Padding.Empty,
            WrapContents = false
        };
        actions.Controls.Add(_pinButton);
        actions.Controls.Add(_closeButton);
        var balance = DetailLabel();
        balance.Anchor = AnchorStyles.Left;
        balance.Margin = Padding.Empty;
        balance.Text = "Codex balance";
        var header = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(balance);
        header.Controls.Add(actions);
        content.Controls.Add(header);
        var gauges = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
            Padding = new Padding(0, 8, 0, 8),
            WrapContents = false
        };
        gauges.Controls.Add(Gauge(_sessionRing, _sessionTitle, _session));
        gauges.Controls.Add(Gauge(_weeklyRing, _weeklyTitle, _weekly));
        foreach (var resetLabel in new[] { _session, _weekly })
        {
            resetLabel.MouseEnter += (sender, _) => HighlightResetLabel((Label)sender!, ((Label)sender!).Cursor == Cursors.Hand);
            resetLabel.MouseLeave += (sender, _) => HighlightResetLabel((Label)sender!, false);
            resetLabel.Click += (sender, _) => ToggleResetTime((Label)sender!);
        }
        _session.Cursor = Cursors.Hand;
        content.Controls.Add(gauges);
        _credits.Margin = Padding.Empty;
        _credits.Padding = new Padding(12, 8, 18, 8);
        _creditsFooter.Controls.Add(_credits);

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = BackColor,
            ColumnCount = 1,
            Margin = Padding.Empty
        };
        layout.Controls.Add(content);
        layout.Controls.Add(_creditsFooter);
        Controls.Add(layout);
        EnableDragging(layout);
        ApplyAccessibilityFont();
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
        _snapshot = snapshot;
        var now = DateTimeOffset.Now;
        var weeklyCanToggle = WeeklyCanToggle(snapshot, now);
        _weekly.Cursor = weeklyCanToggle ? Cursors.Hand : Cursors.Default;
        if (_showWeeklyResetAt && !weeklyCanToggle)
        {
            _showWeeklyResetAt = false;
            SavePreference(WeeklyResetAtPreference, false);
        }
        SetWindow(_sessionTitle, _session, _sessionRing, "Daily", snapshot.SessionWindow, now, _showSessionResetAt);
        SetWindow(_weeklyTitle, _weekly, _weeklyRing, "Weekly", snapshot.WeeklyWindow, now, _showWeeklyResetAt);
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
            ApplyAccessibilityFont();
            if (_snapshot is not null)
            {
                UpdateSnapshot(_snapshot);
            }
            ApplyTheme();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _accessibilityFont?.Dispose();
            _headingFont?.Dispose();
        }
        base.Dispose(disposing);
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
        Margin = new Padding(0, 0, 0, 4),
        MaximumSize = new Size(460, 0)
    };

    private static Label HeadingLabel(string text) => new()
    {
        AutoSize = true,
        Margin = Padding.Empty,
        Text = text
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

    private static Control Gauge(UsageRing ring, Label title, Label label)
    {
        foreach (var text in new[] { title, label })
        {
            text.MinimumSize = new Size(GaugeLabelWidth, 0);
            text.MaximumSize = new Size(GaugeLabelWidth, 0);
            text.TextAlign = ContentAlignment.TopCenter;
        }
        label.Margin = Padding.Empty;

        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Margin = new Padding(0, 0, 10, 0)
        };
        ring.Anchor = AnchorStyles.None;
        panel.Controls.Add(ring);
        panel.Controls.Add(title);
        panel.Controls.Add(label);
        return panel;
    }

    private void ApplyTheme()
    {
        if (SystemInformation.HighContrast)
        {
            ApplyColors(this, SystemColors.Window, SystemColors.WindowText);
            _creditsFooter.BackColor = SystemColors.Window;
            _credits.BackColor = SystemColors.Window;
            _credits.ForeColor = SystemColors.WindowText;
            _borderColor = SystemColors.WindowFrame;
            Invalidate(true);
            return;
        }

        var light = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme",
            1) is not 0;
        var background = light ? Color.White : Color.Black;
        var foreground = light ? Color.Black : Color.White;
        var footerBackground = light ? Color.FromArgb(229, 231, 235) : Color.FromArgb(55, 65, 81);
        var footerForeground = light ? Color.FromArgb(31, 41, 55) : Color.FromArgb(241, 245, 249);

        ApplyColors(this, background, foreground);
        _creditsFooter.BackColor = _credits.BackColor = footerBackground;
        _credits.ForeColor = footerForeground;
        _borderColor = light ? Color.LightGray : Color.DimGray;
        Invalidate(true);
    }

    private void ApplyAccessibilityFont()
    {
        var systemFont = SystemFonts.MessageBoxFont ?? Control.DefaultFont;
        var percent = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Accessibility",
            "TextScaleFactor",
            100) as int? ?? 100;
        percent = Math.Clamp(percent, 100, 225);
        var font = new Font(
            systemFont.FontFamily,
            systemFont.Size * percent / 100f,
            systemFont.Style,
            GraphicsUnit.Point);
        Font = font;
        var headingFont = new Font("Segoe UI Semibold", 11f * percent / 100f, FontStyle.Regular, GraphicsUnit.Point);
        _sessionTitle.Font = _weeklyTitle.Font = headingFont;
        var controlScale = 1 + (percent - 100) / 200f;
        var labelWidth = (int)Math.Round(GaugeLabelWidth * controlScale);
        var ringSize = (int)Math.Round(76 * controlScale);
        var actionSize = (int)Math.Round(24 * percent / 100f);
        _session.MinimumSize = _weekly.MinimumSize = new Size(labelWidth, 0);
        _session.MaximumSize = _weekly.MaximumSize = new Size(labelWidth, 0);
        _sessionRing.Size = _weeklyRing.Size = new Size(ringSize, ringSize);
        _pinButton.Size = _closeButton.Size = new Size(actionSize, actionSize);
        _accessibilityFont?.Dispose();
        _headingFont?.Dispose();
        _accessibilityFont = font;
        _headingFont = headingFont;
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
        if (control is not Button && control != _session && control != _weekly)
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

    private static void HighlightResetLabel(Label label, bool highlighted)
    {
        var background = label.Parent!.BackColor;
        label.BackColor = highlighted
            ? background.GetBrightness() > 0.5f ? Color.LightGray : Color.DarkGray
            : background;
    }

    private void ToggleResetTime(Label label)
    {
        var now = DateTimeOffset.Now;
        if (label == _weekly && (_snapshot is null || !WeeklyCanToggle(_snapshot, now)))
        {
            return;
        }

        if (label == _session)
        {
            _showSessionResetAt = !_showSessionResetAt;
            SavePreference(SessionResetAtPreference, _showSessionResetAt);
        }
        else
        {
            _showWeeklyResetAt = !_showWeeklyResetAt;
            SavePreference(WeeklyResetAtPreference, _showWeeklyResetAt);
        }

        if (_snapshot is not null)
        {
            UpdateSnapshot(_snapshot);
        }
    }

    private static bool WeeklyCanToggle(CodexSnapshot snapshot, DateTimeOffset now) =>
        snapshot.WeeklyWindow?.ResetsAt is { } resetsAt
        && resetsAt > now
        && resetsAt - now < TimeSpan.FromDays(1);

    private static bool ReadPreference(string name) => Registry.GetValue(PreferencesKey, name, 0) is 1;

    private static void SavePreference(string name, bool value)
    {
        try
        {
            Registry.SetValue(PreferencesKey, name, value ? 1 : 0, RegistryValueKind.DWord);
        }
        catch (UnauthorizedAccessException)
        {
            // Preferences are optional; keep the toggle working for this session.
        }
        catch (System.Security.SecurityException)
        {
            // Preferences are optional; keep the toggle working for this session.
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint window, int message, nint parameter, nint data);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    private static void SetWindow(
        Label title,
        Label label,
        UsageRing ring,
        string name,
        LimitWindow? window,
        DateTimeOffset now,
        bool showResetAt)
    {
        title.Visible = label.Visible = ring.Visible = window is not null;
        if (window is null)
        {
            return;
        }

        label.Text = DisplayFormatter.WindowLine(name, window, now, showResetAt)[name.Length..].TrimStart();
        ring.Value = (int)Math.Round(Math.Clamp(window.UsedPercent, 0d, 100d));
    }
}

internal sealed class UsageRing : Control
{
    private const double AnimationDurationMs = 300;
    private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 15 };
    private int _value;
    private float _displayValue;
    private float _animationStartValue;
    private long _animationStartedAt;
    private Font _valueFont;

    public UsageRing(string accessibleName)
    {
        AccessibleName = accessibleName;
        AccessibleRole = AccessibleRole.ProgressBar;
        DoubleBuffered = true;
        _valueFont = ValueFont(Font);
        Margin = new Padding(0, 0, 0, 6);
        Size = new Size(76, 76);
        _animationTimer.Tick += (_, _) => Animate();
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, 0, 100);
            if (_value == next)
            {
                return;
            }

            _value = next;
            AccessibleDescription = $"{_value}% used";
            AccessibilityNotifyClients(AccessibleEvents.ValueChange, -1);

            if (!Visible || !SystemInformation.UIEffectsEnabled)
            {
                _animationTimer.Stop();
                _displayValue = _value;
                Invalidate();
                return;
            }

            _animationStartValue = _displayValue;
            _animationStartedAt = Environment.TickCount64;
            _animationTimer.Start();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
            _valueFont.Dispose();
        }
        base.Dispose(disposing);
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        _valueFont?.Dispose();
        _valueFont = ValueFont(Font);
    }

    private static Font ValueFont(Font font) => new(
        font.FontFamily,
        font.Size * 14f / 9f,
        FontStyle.Bold,
        GraphicsUnit.Point);

    private void Animate()
    {
        var progress = Math.Min(1d, (Environment.TickCount64 - _animationStartedAt) / AnimationDurationMs);
        _displayValue = _animationStartValue + (_value - _animationStartValue) * (float)(1 - Math.Pow(1 - progress, 3));
        Invalidate();

        if (progress >= 1)
        {
            _animationTimer.Stop();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var diameter = Math.Min(Width, Height) - 13;
        var bounds = new Rectangle((Width - diameter) / 2, (Height - diameter) / 2, diameter, diameter);
        var ringColor = SystemInformation.HighContrast ? ForeColor : UsageBandSelector.Select(100 - _value) switch
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
        if (_displayValue > 0)
        {
            e.Graphics.DrawArc(progress, bounds, -90f, 360f * _displayValue / 100f);
        }
        TextRenderer.DrawText(e.Graphics, _value.ToString(), _valueFont, ClientRectangle, ringColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
    }
}
