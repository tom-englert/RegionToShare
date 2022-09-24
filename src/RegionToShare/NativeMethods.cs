// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

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

    public const int WM_NCHITTEST = 0x0084;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject([In] IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPlacement(this IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

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

        public static RECT operator +(RECT rect, Thickness borderSize)
        {
            return new RECT
            {
                Left = rect.Left - (int)borderSize.Left,
                Top = rect.Top - (int)borderSize.Top,
                Right = rect.Right + (int)borderSize.Right,
                Bottom = rect.Bottom + (int)borderSize.Bottom
            };
        }

        public static RECT operator -(RECT rect, Thickness borderSize)
        {
            return new RECT
            {
                Left = rect.Left + (int)borderSize.Left,
                Top = rect.Top + (int)borderSize.Top,
                Right = rect.Right - (int)borderSize.Right,
                Bottom = rect.Bottom - (int)borderSize.Bottom
            };
        }

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

        public static implicit operator Point(POINT p)
        {
            return new Point(p.X, p.Y);
        }

        public static implicit operator POINT(Point p)
        {
            return new POINT((int)Math.Round(p.X), (int)Math.Round(p.Y));
        }

        public override string ToString()
        {
            return ((Point)this).ToString();
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

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPLACEMENT
    {
        public int Length;

        public int Flags;

        public uint ShowCmd;

        public POINT MinPosition;

        public POINT MaxPosition;

        public RECT NormalPosition;

        /// <summary>
        /// Gets the default (empty) value.
        /// </summary>
        public static WINDOWPLACEMENT Default
        {
            get
            {
                var result = new WINDOWPLACEMENT();
                result.Length = Marshal.SizeOf(result);
                return result;
            }
        }
    }
}