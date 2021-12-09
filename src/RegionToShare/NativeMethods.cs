// ReSharper disable All

using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;

namespace RegionToShare;

internal static class NativeMethods
{
    public static readonly IntPtr HWND_TOP = new(0);
    public static readonly IntPtr HWND_BOTTOM = new(1);
    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOMOVE = 0x0002;
    public const int SWP_SHOWWINDOW = 0x0040;
    public const int SWP_HIDEWINDOW = 0x0080;
    public const int SWP_NOACTIVATE = 0x0010;
    public const int SWP_NOZORDER = 0x0004;

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    public const int WM_NCHITTEST = 0x0084;
    public const int CURSOR_SHOWING = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [DllImport("user32.dll")]
    public static extern bool GetCursorInfo(ref CURSORINFO pci);

    [DllImport("user32.dll")]
    public static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    public static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public POINT TopLeft => new() { X = Left, Y = Top };

        public POINT BottomRight => new() { X = Right, Y = Bottom };

        public int Width => Right - Left;

        public int Height => Bottom - Top;

        public static implicit operator Rect(RECT r)
        {
            return new Rect(r.TopLeft, r.BottomRight);
        }

        public static implicit operator RECT(Rect r)
        {
            return new RECT { Left = (int)r.Left, Top = (int)r.Top, Right = (int)r.Right, Bottom = (int)r.Bottom };
        }

        public override string ToString()
        {
            return ((Rect)this).ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static POINT operator +(POINT p1, POINT p2)
        {
            return new POINT { X = p1.X + p2.X, Y = p1.Y + p2.Y };
        }

        public static POINT operator -(POINT p1, POINT p2)
        {
            return new POINT { X = p1.X - p2.X, Y = p1.Y - p2.Y };
        }

        public static implicit operator System.Windows.Point(POINT p)
        {
            return new System.Windows.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Windows.Point p)
        {
            return new POINT((int)Math.Round(p.X), (int)Math.Round(p.Y));
        }

        public override string ToString()
        {
            return ((System.Windows.Point)this).ToString();
        }
    }

    public enum HitTest
    {
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Nowhere = 0,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Client = 1,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Caption = 2,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        SysMenu = 3,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        GrowBox = 4,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Size = GrowBox,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Menu = 5,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        HScroll = 6,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        VScroll = 7,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        MinButton = 8,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        MaxButton = 9,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Left = 10,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Right = 11,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Top = 12,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        TopLeft = 13,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        TopRight = 14,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Bottom = 15,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        BottomLeft = 16,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        BottomRight = 17,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Border = 18,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Object = 19,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Close = 20,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Help = 21,
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Error = (-2),
        /// <summary>See documentation of WM_NCHITTEST</summary>
        Transparent = (-1),
    }
}