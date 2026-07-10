using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

class Program
{
    static void Main(string[] args)
    {
        var outputDir = args.Length > 0 ? args[0] : "../../src/TimeTracker.App";
        var sizes = new[] { 16, 32, 48, 64, 128, 256 };
        var icoPath = Path.Combine(outputDir, "app.ico");

        var pngDataList = new List<byte[]>();
        var offsetsList = new List<int>();
        var dataOffset = 6 + sizes.Length * 16;

        for (int i = 0; i < sizes.Length; i++)
        {
            var bmp = CreateClockIcon(sizes[i]);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            pngDataList.Add(ms.ToArray());
            offsetsList.Add(dataOffset);
            dataOffset += (int)ms.Length;
            bmp.Dispose();
        }

        using var fs = File.Create(icoPath);
        using var bw = new BinaryWriter(fs);

        bw.Write((ushort)0);
        bw.Write((ushort)1);
        bw.Write((ushort)sizes.Length);

        for (int i = 0; i < sizes.Length; i++)
        {
            bw.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
            bw.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((ushort)1);
            bw.Write((ushort)32);
            bw.Write((int)pngDataList[i].Length);
            bw.Write((int)offsetsList[i]);
        }

        for (int i = 0; i < pngDataList.Count; i++)
            bw.Write(pngDataList[i]);

        Console.WriteLine($"Icon created: {icoPath} ({sizes.Length} sizes)");
    }

    static Bitmap CreateClockIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Color.Transparent);

        float cx = size / 2f, cy = size / 2f;
        float radius = size * 0.44f;

        using var accentBrush = new SolidBrush(Color.FromArgb(108, 114, 203));
        g.FillEllipse(accentBrush, cx - radius, cy - radius, radius * 2, radius * 2);

        float innerR = radius * 0.80f;
        using var bgBrush = new SolidBrush(Color.FromArgb(22, 24, 28));
        g.FillEllipse(bgBrush, cx - innerR, cy - innerR, innerR * 2, innerR * 2);

        float tickOuter = radius * 0.70f;
        float tickInnerMajor = radius * 0.52f;
        float tickInnerMinor = radius * 0.60f;

        for (int i = 0; i < 12; i++)
        {
            double angle = (i * 30 - 90) * Math.PI / 180;
            bool isMajor = i % 3 == 0;
            float tickInner = isMajor ? tickInnerMajor : tickInnerMinor;
            float tickWidth = size * (isMajor ? 0.028f : 0.014f);

            using var tickPen = new Pen(Color.FromArgb(160, 160, 172), tickWidth) { EndCap = LineCap.Round };
            float x1 = cx + (float)(Math.Cos(angle) * tickOuter);
            float y1 = cy + (float)(Math.Sin(angle) * tickOuter);
            float x2 = cx + (float)(Math.Cos(angle) * tickInner);
            float y2 = cy + (float)(Math.Sin(angle) * tickInner);
            g.DrawLine(tickPen, x1, y1, x2, y2);
        }

        using var hourPen = new Pen(Color.FromArgb(240, 240, 242), size * 0.04f) { EndCap = LineCap.Round, StartCap = LineCap.Round };
        double hourAngle = (310 - 90) * Math.PI / 180;
        g.DrawLine(hourPen, cx, cy, cx + (float)(Math.Cos(hourAngle) * radius * 0.36f), cy + (float)(Math.Sin(hourAngle) * radius * 0.36f));

        using var minPen = new Pen(Color.FromArgb(240, 240, 242), size * 0.025f) { EndCap = LineCap.Round, StartCap = LineCap.Round };
        double minAngle = (60 - 90) * Math.PI / 180;
        g.DrawLine(minPen, cx, cy, cx + (float)(Math.Cos(minAngle) * radius * 0.52f), cy + (float)(Math.Sin(minAngle) * radius * 0.52f));

        using var centerBrush = new SolidBrush(Color.FromArgb(108, 114, 203));
        float centerR = size * 0.04f;
        g.FillEllipse(centerBrush, cx - centerR, cy - centerR, centerR * 2, centerR * 2);

        return bmp;
    }
}
