using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace IPScan.UI;

/// <summary>
/// Generates crisp, modern, vector-rendered icons for service actions and context menus.
/// Scalable across standard and high-DPI displays without distortion.
/// </summary>
public static class MenuIconHelper
{
    private static readonly Dictionary<int, Bitmap> _httpCache = new();
    private static readonly Dictionary<int, Bitmap> _httpsCache = new();
    private static readonly Dictionary<int, Bitmap> _vncCache = new();
    private static readonly Dictionary<int, Bitmap> _sharedFolderCache = new();
    private static readonly Dictionary<int, Bitmap> _copyIpCache = new();
    private static readonly Dictionary<int, Bitmap> _copyMacCache = new();
    private static readonly Dictionary<int, Bitmap> _copyRowCache = new();
    private static readonly object _syncLock = new();

    /// <summary>
    /// Creates or returns a cached HTTP Web Globe icon.
    /// </summary>
    public static Bitmap CreateHttpIcon(int size = 16)
    {
        lock (_syncLock)
        {
            if (_httpCache.TryGetValue(size, out var cached)) return (Bitmap)cached.Clone();
            var bmp = RenderHttpIcon(size);
            _httpCache[size] = bmp;
            return (Bitmap)bmp.Clone();
        }
    }

    /// <summary>
    /// Creates or returns a cached HTTPS Secure Padlock icon.
    /// </summary>
    public static Bitmap CreateHttpsIcon(int size = 16)
    {
        lock (_syncLock)
        {
            if (_httpsCache.TryGetValue(size, out var cached)) return (Bitmap)cached.Clone();
            var bmp = RenderHttpsIcon(size);
            _httpsCache[size] = bmp;
            return (Bitmap)bmp.Clone();
        }
    }

    /// <summary>
    /// Creates or returns a cached VNC Viewer / Remote Desktop icon.
    /// </summary>
    public static Bitmap CreateVncIcon(int size = 16)
    {
        lock (_syncLock)
        {
            if (_vncCache.TryGetValue(size, out var cached)) return (Bitmap)cached.Clone();
            var bmp = RenderVncIcon(size);
            _vncCache[size] = bmp;
            return (Bitmap)bmp.Clone();
        }
    }

    /// <summary>
    /// Creates or returns a cached Shared Folders (Windows SMB) icon.
    /// </summary>
    public static Bitmap CreateSharedFolderIcon(int size = 16)
    {
        lock (_syncLock)
        {
            if (_sharedFolderCache.TryGetValue(size, out var cached)) return (Bitmap)cached.Clone();
            var bmp = RenderSharedFolderIcon(size);
            _sharedFolderCache[size] = bmp;
            return (Bitmap)bmp.Clone();
        }
    }

    /// <summary>
    /// Creates or returns a cached Copy IP Address icon.
    /// </summary>
    public static Bitmap CreateCopyIpIcon(int size = 16)
    {
        lock (_syncLock)
        {
            if (_copyIpCache.TryGetValue(size, out var cached)) return (Bitmap)cached.Clone();
            var bmp = RenderCopyIpIcon(size);
            _copyIpCache[size] = bmp;
            return (Bitmap)bmp.Clone();
        }
    }

    /// <summary>
    /// Creates or returns a cached Copy MAC Address icon.
    /// </summary>
    public static Bitmap CreateCopyMacIcon(int size = 16)
    {
        lock (_syncLock)
        {
            if (_copyMacCache.TryGetValue(size, out var cached)) return (Bitmap)cached.Clone();
            var bmp = RenderCopyMacIcon(size);
            _copyMacCache[size] = bmp;
            return (Bitmap)bmp.Clone();
        }
    }

    /// <summary>
    /// Creates or returns a cached Copy Row Details icon.
    /// </summary>
    public static Bitmap CreateCopyRowDetailsIcon(int size = 16)
    {
        lock (_syncLock)
        {
            if (_copyRowCache.TryGetValue(size, out var cached)) return (Bitmap)cached.Clone();
            var bmp = RenderCopyRowDetailsIcon(size);
            _copyRowCache[size] = bmp;
            return (Bitmap)bmp.Clone();
        }
    }

    private static Graphics CreateConfiguredGraphics(Bitmap bmp)
    {
        var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        return g;
    }

    private static Bitmap RenderHttpIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = CreateConfiguredGraphics(bmp);
        float s = size / 16f;

        float x = 2f * s;
        float y = 2f * s;
        float d = 12f * s;

        using (var circlePath = new GraphicsPath())
        {
            circlePath.AddEllipse(x, y, d, d);

            // Fill globe with gradient
            using (var brush = new LinearGradientBrush(
                new PointF(x, y), new PointF(x + d, y + d),
                Color.FromArgb(41, 182, 246), Color.FromArgb(2, 119, 189)))
            {
                g.FillPath(brush, circlePath);
            }

            // Draw latitude and longitude grids within the clipped globe
            g.SetClip(circlePath);

            using (var gridPen = new Pen(Color.FromArgb(235, 255, 255, 255), Math.Max(1f, 1.1f * s)))
            {
                // Equator
                g.DrawLine(gridPen, x, y + d / 2f, x + d, y + d / 2f);
                // Central vertical meridian
                g.DrawLine(gridPen, x + d / 2f, y, x + d / 2f, y + d);
                // Vertical meridian oval
                g.DrawEllipse(gridPen, 5.2f * s, y, 5.6f * s, d);
                // Upper latitude
                g.DrawLine(gridPen, 0, 5f * s, 16f * s, 5f * s);
                // Lower latitude
                g.DrawLine(gridPen, 0, 11f * s, 16f * s, 11f * s);
            }

            g.ResetClip();

            // Crisp outer border
            using var borderPen = new Pen(Color.FromArgb(1, 87, 155), Math.Max(1f, 1.2f * s));
            g.DrawEllipse(borderPen, x, y, d, d);
        }

        return bmp;
    }

    private static Bitmap RenderHttpsIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = CreateConfiguredGraphics(bmp);
        float s = size / 16f;

        // 1. Shackle (inverted U)
        using (var shacklePen = new Pen(Color.FromArgb(120, 144, 156), Math.Max(1.5f, 2.2f * s)))
        {
            shacklePen.StartCap = LineCap.Flat;
            shacklePen.EndCap = LineCap.Flat;

            using var shacklePath = new GraphicsPath();
            shacklePath.AddArc(5.2f * s, 1.8f * s, 5.6f * s, 5.6f * s, 180, 180);
            shacklePath.AddLine(10.8f * s, 4.6f * s, 10.8f * s, 7.5f * s);
            g.DrawPath(shacklePen, shacklePath);
            g.DrawLine(shacklePen, 5.2f * s, 4.6f * s, 5.2f * s, 7.5f * s);
        }

        // 2. Padlock body
        float bodyX = 2.5f * s;
        float bodyY = 6.5f * s;
        float bodyW = 11f * s;
        float bodyH = 8.5f * s;
        float r = 2f * s;

        using (var bodyPath = CreateRoundedRectanglePath(bodyX, bodyY, bodyW, bodyH, r))
        {
            using (var bodyBrush = new LinearGradientBrush(
                new PointF(bodyX, bodyY), new PointF(bodyX, bodyY + bodyH),
                Color.FromArgb(46, 125, 50), Color.FromArgb(27, 94, 32)))
            {
                g.FillPath(bodyBrush, bodyPath);
            }

            using var borderPen = new Pen(Color.FromArgb(18, 65, 22), Math.Max(1f, 1.1f * s));
            g.DrawPath(borderPen, bodyPath);
        }

        // 3. Keyhole (Golden/White accent)
        using (var keyholeBrush = new SolidBrush(Color.FromArgb(255, 238, 88)))
        {
            g.FillEllipse(keyholeBrush, 7f * s, 8.5f * s, 2.2f * s, 2.2f * s);

            PointF[] keyholeStem =
            {
                new(7.4f * s, 9.8f * s),
                new(8.6f * s, 9.8f * s),
                new(9f * s, 12.2f * s),
                new(7f * s, 12.2f * s)
            };
            g.FillPolygon(keyholeBrush, keyholeStem);
        }

        return bmp;
    }

    private static Bitmap RenderVncIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = CreateConfiguredGraphics(bmp);
        float s = size / 16f;

        // 1. Stand Base and Neck
        using (var standPen = new Pen(Color.FromArgb(69, 90, 100), Math.Max(1.5f, 2f * s)))
        {
            standPen.StartCap = LineCap.Round;
            standPen.EndCap = LineCap.Round;
            // Neck
            g.DrawLine(standPen, 8f * s, 11f * s, 8f * s, 14f * s);
            // Base
            g.DrawLine(standPen, 5f * s, 14.2f * s, 11f * s, 14.2f * s);
        }

        // 2. Monitor Outer Frame
        float monX = 1.5f * s;
        float monY = 1.5f * s;
        float monW = 13f * s;
        float monH = 9.8f * s;
        float r = 1.6f * s;

        using (var framePath = CreateRoundedRectanglePath(monX, monY, monW, monH, r))
        {
            using var frameBrush = new SolidBrush(Color.FromArgb(38, 50, 56));
            g.FillPath(frameBrush, framePath);
        }

        // 3. Screen display
        float scrX = 2.7f * s;
        float scrY = 2.7f * s;
        float scrW = 10.6f * s;
        float scrH = 7.4f * s;

        using (var screenBrush = new LinearGradientBrush(
            new PointF(scrX, scrY), new PointF(scrX, scrY + scrH),
            Color.FromArgb(0, 151, 167), Color.FromArgb(0, 96, 100)))
        {
            g.FillRectangle(screenBrush, scrX, scrY, scrW, scrH);
        }

        // 4. Remote Cursor Arrow (indicating remote access)
        using var cursorPath = new GraphicsPath();
        cursorPath.AddPolygon(new PointF[]
        {
            new(4.5f * s, 3.8f * s),
            new(4.5f * s, 8.4f * s),
            new(6.4f * s, 7.3f * s),
            new(7.8f * s, 9.4f * s),
            new(8.8f * s, 8.7f * s),
            new(7.4f * s, 6.7f * s),
            new(9.2f * s, 6.4f * s)
        });

        using (var cursorBrush = new SolidBrush(Color.White))
        {
            g.FillPath(cursorBrush, cursorPath);
        }
        using (var cursorOutline = new Pen(Color.FromArgb(0, 77, 64), Math.Max(0.8f, 0.9f * s)))
        {
            g.DrawPath(cursorOutline, cursorPath);
        }

        return bmp;
    }

    private static Bitmap RenderSharedFolderIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = CreateConfiguredGraphics(bmp);
        float s = size / 16f;

        // 1. Folder Back Tab
        using (var tabPath = CreateRoundedRectanglePath(1.5f * s, 2.2f * s, 5.5f * s, 3f * s, 1f * s))
        {
            using var tabBrush = new SolidBrush(Color.FromArgb(255, 179, 0));
            g.FillPath(tabBrush, tabPath);
        }

        // 2. Folder Back Body
        using (var backPath = CreateRoundedRectanglePath(1.5f * s, 4f * s, 13f * s, 9.5f * s, 1.5f * s))
        {
            using var backBrush = new SolidBrush(Color.FromArgb(255, 179, 0));
            g.FillPath(backBrush, backPath);
            using var borderPen = new Pen(Color.FromArgb(245, 127, 23), Math.Max(0.8f, 1f * s));
            g.DrawPath(borderPen, backPath);
        }

        // 3. Folder Front Flap (slightly open)
        PointF[] flapPoints =
        {
            new(1.5f * s, 6.8f * s),
            new(14.5f * s, 6.8f * s),
            new(13.2f * s, 13.5f * s),
            new(1.5f * s, 13.5f * s)
        };
        using (var flapBrush = new LinearGradientBrush(
            new PointF(1.5f * s, 6.8f * s), new PointF(1.5f * s, 13.5f * s),
            Color.FromArgb(255, 213, 79), Color.FromArgb(255, 160, 0)))
        {
            g.FillPolygon(flapBrush, flapPoints);
        }
        using (var flapPen = new Pen(Color.FromArgb(245, 127, 23), Math.Max(0.8f, 1f * s)))
        {
            g.DrawPolygon(flapPen, flapPoints);
        }

        // 4. Network Share Link (SMB Network nodes badge at bottom right)
        using (var linkPen = new Pen(Color.FromArgb(25, 118, 210), Math.Max(1.2f, 1.6f * s)))
        {
            g.DrawLine(linkPen, 7.5f * s, 11.8f * s, 12.5f * s, 11.8f * s);
        }

        using (var nodeBrush = new SolidBrush(Color.FromArgb(25, 118, 210)))
        using (var nodeBorder = new Pen(Color.White, Math.Max(0.8f, 0.9f * s)))
        {
            // Left node
            g.FillEllipse(nodeBrush, 6.2f * s, 10.5f * s, 2.6f * s, 2.6f * s);
            g.DrawEllipse(nodeBorder, 6.2f * s, 10.5f * s, 2.6f * s, 2.6f * s);

            // Right node
            g.FillEllipse(nodeBrush, 11.2f * s, 10.5f * s, 2.6f * s, 2.6f * s);
            g.DrawEllipse(nodeBorder, 11.2f * s, 10.5f * s, 2.6f * s, 2.6f * s);
        }

        return bmp;
    }

    private static Bitmap RenderCopyIpIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = CreateConfiguredGraphics(bmp);
        float s = size / 16f;

        // 1. Back Sheet
        using (var backPath = CreateRoundedRectanglePath(5f * s, 1.5f * s, 9.5f * s, 10.5f * s, 1.2f * s))
        {
            using var backBrush = new SolidBrush(Color.FromArgb(236, 239, 241));
            g.FillPath(backBrush, backPath);
            using var backPen = new Pen(Color.FromArgb(176, 190, 197), Math.Max(0.8f, 1f * s));
            g.DrawPath(backPen, backPath);
        }

        // 2. Front Sheet
        using (var frontPath = CreateRoundedRectanglePath(1.5f * s, 4f * s, 9.5f * s, 10.5f * s, 1.2f * s))
        {
            using var frontBrush = new SolidBrush(Color.White);
            g.FillPath(frontBrush, frontPath);
            using var frontPen = new Pen(Color.FromArgb(25, 118, 210), Math.Max(1f, 1.2f * s));
            g.DrawPath(frontPen, frontPath);
        }

        // 3. IP address lines
        using var linePen1 = new Pen(Color.FromArgb(25, 118, 210), Math.Max(1f, 1.2f * s));
        using var linePen2 = new Pen(Color.FromArgb(66, 165, 245), Math.Max(1f, 1.2f * s));
        using var linePen3 = new Pen(Color.FromArgb(144, 202, 249), Math.Max(1f, 1.2f * s));

        g.DrawLine(linePen1, 3.5f * s, 6.8f * s, 8.5f * s, 6.8f * s);
        g.DrawLine(linePen2, 3.5f * s, 9.2f * s, 7.5f * s, 9.2f * s);
        g.DrawLine(linePen3, 3.5f * s, 11.6f * s, 6f * s, 11.6f * s);

        return bmp;
    }

    private static Bitmap RenderCopyMacIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = CreateConfiguredGraphics(bmp);
        float s = size / 16f;

        // 1. Back Sheet
        using (var backPath = CreateRoundedRectanglePath(4.5f * s, 1.5f * s, 10f * s, 10.5f * s, 1.2f * s))
        {
            using var backBrush = new SolidBrush(Color.FromArgb(236, 239, 241));
            g.FillPath(backBrush, backPath);
            using var backPen = new Pen(Color.FromArgb(176, 190, 197), Math.Max(0.8f, 1f * s));
            g.DrawPath(backPen, backPath);
        }

        // 2. Front Card / Adapter
        using (var frontPath = CreateRoundedRectanglePath(1.5f * s, 4f * s, 10f * s, 10.5f * s, 1.2f * s))
        {
            using var frontBrush = new SolidBrush(Color.White);
            g.FillPath(frontBrush, frontPath);
            using var frontPen = new Pen(Color.FromArgb(0, 150, 136), Math.Max(1f, 1.2f * s)); // Teal
            g.DrawPath(frontPen, frontPath);
        }

        // 3. Network pins on top
        using var pinBrush = new SolidBrush(Color.FromArgb(255, 179, 0));
        g.FillRectangle(pinBrush, 3.5f * s, 5.5f * s, 1.2f * s, 2f * s);
        g.FillRectangle(pinBrush, 5.5f * s, 5.5f * s, 1.2f * s, 2f * s);
        g.FillRectangle(pinBrush, 7.5f * s, 5.5f * s, 1.2f * s, 2f * s);

        // 4. Hex MAC indicator lines
        using var linePen1 = new Pen(Color.FromArgb(0, 150, 136), Math.Max(1f, 1.2f * s));
        using var linePen2 = new Pen(Color.FromArgb(77, 182, 172), Math.Max(1f, 1.2f * s));

        g.DrawLine(linePen1, 3.5f * s, 9.5f * s, 9.5f * s, 9.5f * s);
        g.DrawLine(linePen2, 3.5f * s, 12f * s, 7.5f * s, 12f * s);

        return bmp;
    }

    private static Bitmap RenderCopyRowDetailsIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = CreateConfiguredGraphics(bmp);
        float s = size / 16f;

        float sheetX = 1.8f * s;
        float sheetY = 1.5f * s;
        float sheetW = 12.4f * s;
        float sheetH = 13f * s;
        float r = 1.4f * s;

        // 1. Outer Sheet
        using (var sheetPath = CreateRoundedRectanglePath(sheetX, sheetY, sheetW, sheetH, r))
        {
            using var sheetBrush = new SolidBrush(Color.White);
            g.FillPath(sheetBrush, sheetPath);

            // 2. Top Header Bar
            g.SetClip(sheetPath);
            using (var headerBrush = new SolidBrush(Color.FromArgb(0, 137, 123)))
            {
                g.FillRectangle(headerBrush, sheetX, sheetY, sheetW, 3.8f * s);
            }

            // 3. Table Dividing Lines
            using (var gridPen = new Pen(Color.FromArgb(207, 216, 220), Math.Max(0.8f, 1f * s)))
            {
                // Vertical column divider
                g.DrawLine(gridPen, 6.2f * s, sheetY, 6.2f * s, sheetY + sheetH);
                // Row dividers
                g.DrawLine(gridPen, sheetX, 8.2f * s, sheetX + sheetW, 8.2f * s);
                g.DrawLine(gridPen, sheetX, 11.2f * s, sheetX + sheetW, 11.2f * s);
            }

            // 4. Data rows preview bars
            using var cellPen1 = new Pen(Color.FromArgb(120, 144, 156), Math.Max(0.8f, 1.1f * s));
            using var cellPen2 = new Pen(Color.FromArgb(144, 164, 174), Math.Max(0.8f, 1.1f * s));

            g.DrawLine(cellPen1, 3.2f * s, 6.4f * s, 5.2f * s, 6.4f * s);
            g.DrawLine(cellPen1, 7.6f * s, 6.4f * s, 12.2f * s, 6.4f * s);

            g.DrawLine(cellPen2, 3.2f * s, 9.7f * s, 5.2f * s, 9.7f * s);
            g.DrawLine(cellPen2, 7.6f * s, 9.7f * s, 11.2f * s, 9.7f * s);

            g.DrawLine(cellPen2, 3.2f * s, 12.7f * s, 4.8f * s, 12.7f * s);
            g.DrawLine(cellPen2, 7.6f * s, 12.7f * s, 10.2f * s, 12.7f * s);

            g.ResetClip();

            // Border
            using var borderPen = new Pen(Color.FromArgb(69, 90, 100), Math.Max(1f, 1.2f * s));
            g.DrawPath(borderPen, sheetPath);
        }

        return bmp;
    }

    private static GraphicsPath CreateRoundedRectanglePath(float x, float y, float w, float h, float r)
    {
        var path = new GraphicsPath();
        float d = r * 2f;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
