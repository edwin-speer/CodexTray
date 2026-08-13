using System.Drawing.Drawing2D;

namespace CodexTray;

internal static class NotificationIconRenderer
{
    public static Icon Render(Icon baseIcon)
    {
        using var bitmap = new Bitmap(64, 64);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.Clear(Color.Transparent);
        graphics.DrawIcon(baseIcon, new Rectangle(1, 1, 56, 56));

        using var badge = new SolidBrush(Color.FromArgb(220, 38, 38));
        using var badgeBorder = new Pen(Color.White, 2.5f);
        graphics.FillEllipse(badge, 38, 36, 25, 25);
        graphics.DrawEllipse(badgeBorder, 38, 36, 25, 25);
        using var font = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Point);
        using var text = new SolidBrush(Color.White);
        var size = graphics.MeasureString("1", font);
        graphics.DrawString("1", font, text, 50.5f - size.Width / 2f, 48f - size.Height / 2f);
        return IconFactory.FromBitmap(bitmap);
    }
}
