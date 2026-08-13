using System.Drawing.Drawing2D;
using CodexTray.Core;

namespace CodexTray;

internal static class TrayIconRenderer
{
    public static Icon Render(double? remainingPercent)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var color = UsageBandSelector.Select(remainingPercent) switch
        {
            UsageBand.Green => Color.FromArgb(34, 197, 94),
            UsageBand.Amber => Color.FromArgb(245, 158, 11),
            UsageBand.Red => Color.FromArgb(239, 68, 68),
            _ => Color.FromArgb(100, 116, 139)
        };
        using var shadow = new SolidBrush(Color.FromArgb(90, 15, 23, 42));
        graphics.FillEllipse(shadow, 1, 2, 30, 30);
        using var background = new LinearGradientBrush(
            new Rectangle(2, 1, 28, 28),
            ControlPaint.Light(color, 0.15f),
            ControlPaint.Dark(color, 0.08f),
            LinearGradientMode.Vertical);
        graphics.FillEllipse(background, 2, 1, 28, 28);
        using var border = new Pen(Color.FromArgb(220, 255, 255, 255), 1.2f);
        graphics.DrawEllipse(border, 2.5f, 1.5f, 27f, 27f);

        var label = remainingPercent is { } value
            ? Math.Round(value).ToString("0")
            : "?";
        using var font = new Font("Segoe UI", label.Length >= 3 ? 8.5f : 10.5f, FontStyle.Bold, GraphicsUnit.Point);
        using var foreground = new SolidBrush(Color.White);
        var size = graphics.MeasureString(label, font);
        graphics.DrawString(label, font, foreground, (32 - size.Width) / 2f, (32 - size.Height) / 2f - 0.5f);

        return IconFactory.FromBitmap(bitmap);
    }
}
