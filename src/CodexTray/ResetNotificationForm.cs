using CodexTray.Core;

namespace CodexTray;

internal sealed class ResetNotificationForm : Form
{
    private readonly Icon _notificationIcon;

    public ResetNotificationForm(IReadOnlyList<UsageNotification> notifications)
    {
        using var applicationIcon = Brand.LoadApplicationIcon();
        _notificationIcon = NotificationIconRenderer.Render(applicationIcon);

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(248, 250, 252);
        ClientSize = new Size(430, 205);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Icon = _notificationIcon;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Codex Tray · 1 notification";

        var title = notifications.Count == 1 ? notifications[0].Title : "Codex usage updated";
        var body = string.Join(Environment.NewLine, notifications.Select(item => item.Message));
        var titleLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 13f, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(15, 23, 42),
            Location = new Point(24, 20),
            Text = title
        };
        var bodyLabel = new Label
        {
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(51, 65, 85),
            Location = new Point(25, 58),
            Size = new Size(378, 54),
            Text = body
        };
        var openButton = new Button
        {
            AutoSize = true,
            Location = new Point(24, 128),
            Padding = new Padding(8, 2, 8, 2),
            Text = "Open Codex usage"
        };
        openButton.Click += (_, _) =>
        {
            Brand.OpenUrl(Brand.CodexUsageUrl);
            Close();
        };
        var dismissButton = new Button
        {
            AutoSize = true,
            Location = new Point(180, 128),
            Padding = new Padding(8, 2, 8, 2),
            Text = "Dismiss"
        };
        dismissButton.Click += (_, _) => Close();
        var footer = new LinkLabel
        {
            AutoSize = true,
            LinkColor = Color.FromArgb(3, 105, 161),
            Location = new Point(25, 178),
            Text = "Codex Tray by vCloudInfo.com"
        };
        footer.LinkClicked += (_, _) => Brand.OpenUrl(Brand.BlogUrl);
        Controls.AddRange([titleLabel, bodyLabel, openButton, dismissButton, footer]);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Icon = null;
            _notificationIcon.Dispose();
        }
        base.Dispose(disposing);
    }

}
