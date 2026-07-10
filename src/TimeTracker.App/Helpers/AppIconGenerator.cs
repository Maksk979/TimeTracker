using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TimeTracker.App.Helpers;

/// <summary>
/// Генерирует кастомную иконку приложения: стилизованные часы на фирменном фоне.
/// Используется для окна, трея и taskbar.
/// </summary>
public static class AppIconGenerator
{
    private static readonly Color AccentColor = Color.FromArgb(108, 114, 203); // #6C72CB
    private static readonly Color bgColor = Color.FromArgb(22, 24, 28);       // #16181C

    /// <summary>Создаёт иконку нужного размера.</summary>
    public static Icon CreateIcon(int size = 256)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        var cx = size / 2f;
        var cy = size / 2f;
        var radius = size * 0.42f;

        // Фон — круг с акцентным цветом
        using var bgBrush = new SolidBrush(AccentColor);
        g.FillEllipse(bgBrush, cx - radius, cy - radius, radius * 2, radius * 2);

        // Внутренний круг — тёмный
        var innerR = radius * 0.78f;
        using var innerBrush = new SolidBrush(bgColor);
        g.FillEllipse(innerBrush, cx - innerR, cy - innerR, innerR * 2, innerR * 2);

        // Циферблат — засечки
        using var tickPen = new Pen(Color.FromArgb(160, 160, 172), size * 0.018f); // #A0A4AC
        var tickOuter = radius * 0.68f;
        var tickInnerMajor = radius * 0.52f;
        var tickInnerMinor = radius * 0.58f;

        for (int i = 0; i < 12; i++)
        {
            var angle = (i * 30 - 90) * Math.PI / 180;
            var isMajor = i % 3 == 0;
            var tickInner = isMajor ? tickInnerMajor : tickInnerMinor;
            var tickWidth = isMajor ? size * 0.025f : size * 0.012f;
            tickPen.Width = tickWidth;

            var x1 = cx + (float)(Math.Cos(angle) * tickOuter);
            var y1 = cy + (float)(Math.Sin(angle) * tickOuter);
            var x2 = cx + (float)(Math.Cos(angle) * tickInner);
            var y2 = cy + (float)(Math.Sin(angle) * tickInner);
            g.DrawLine(tickPen, x1, y1, x2, y2);
        }

        // Стрелки — часовая и минутная
        using var hourPen = new Pen(Color.FromArgb(240, 240, 242), size * 0.035f) // #F0F0F2
        {
            EndCap = LineCap.Round,
            StartCap = LineCap.Round,
        };
        using var minutePen = new Pen(Color.FromArgb(240, 240, 242), size * 0.022f)
        {
            EndCap = LineCap.Round,
            StartCap = LineCap.Round,
        };

        // Часовая стрелка — 10:10 (красивый угол)
        var hourAngle = (310 - 90) * Math.PI / 180; // 10 часов = 300°, +10 минут = 310°
        var hourLen = radius * 0.35f;
        g.DrawLine(hourPen, cx, cy,
            cx + (float)(Math.Cos(hourAngle) * hourLen),
            cy + (float)(Math.Sin(hourAngle) * hourLen));

        // Минутная стрелка
        var minuteAngle = (60 - 90) * Math.PI / 180; // 10 минут = 60°
        var minuteLen = radius * 0.50f;
        g.DrawLine(minutePen, cx, cy,
            cx + (float)(Math.Cos(minuteAngle) * minuteLen),
            cy + (float)(Math.Sin(minuteAngle) * minuteLen));

        // Центральная точка
        using var centerBrush = new SolidBrush(AccentColor);
        var centerR = size * 0.035f;
        g.FillEllipse(centerBrush, cx - centerR, cy - centerR, centerR * 2, centerR * 2);

        return Icon.FromHandle(bmp.GetHicon());
    }

    /// <summary>Конвертирует Icon в BitmapSource для WPF Window.Icon.</summary>
    public static BitmapSource ToBitmapSource(Icon icon)
    {
        return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
    }
}
