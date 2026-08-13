using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace CodexTray;

internal static class TrayIconRenderer
{
    public static Icon Render(double? remainingPercent)
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var color = remainingPercent switch
        {
            null => Color.FromArgb(100, 116, 139),
            > 50 => Color.FromArgb(34, 197, 94),
            > 20 => Color.FromArgb(245, 158, 11),
            _ => Color.FromArgb(239, 68, 68)
        };
        using var background = new SolidBrush(color);
        graphics.FillEllipse(background, 1, 1, 30, 30);

        var label = remainingPercent is { } value
            ? Math.Round(value).ToString("0")
            : "?";
        using var font = new Font("Segoe UI", label.Length >= 3 ? 8.5f : 10.5f, FontStyle.Bold, GraphicsUnit.Point);
        using var foreground = new SolidBrush(Color.White);
        var size = graphics.MeasureString(label, font);
        graphics.DrawString(label, font, foreground, (32 - size.Width) / 2f, (32 - size.Height) / 2f - 0.5f);

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);
}

