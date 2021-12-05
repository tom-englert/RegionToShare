using System;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using TomsToolbox.Wpf;
using TomsToolbox.Essentials;
using System.Threading;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for RecordingWindow.xaml
    /// </summary>
    public partial class RecordingWindow : Window
    {
        public static readonly Thickness BorderSize = new(4);

        private readonly HighResolutionTimer _timer;
        private readonly Window _mainWindow;
        private readonly IntPtr _renderTargetHandle;
        private readonly IntPtr _desktopWindowHandle;

        private Rectangle _nativeWindowRect;
        private System.Windows.Size _designUnits = new(1, 1);
        private int _timerMutex;

        public RecordingWindow(Window mainWindow, IntPtr renderTargetHandle, int framesPerSecond = 15)
        {
            InitializeComponent();

            _mainWindow = mainWindow;
            _renderTargetHandle = renderTargetHandle;
            _desktopWindowHandle = GetDesktopWindow();

            _timer = new(Timer_Tick, TimeSpan.FromSeconds(1.0 / framesPerSecond));
            _timer.Start();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var messageSource = (HwndSource?)PresentationSource.FromDependencyObject(this) ?? throw new InvalidOperationException("Window needs to be initialized");
            messageSource.AddHook(WindowProc);

            _designUnits = this.GetDesignUnitSize();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            _mainWindow.Width = Width - BorderSize.Left - BorderSize.Right;
            _mainWindow.Height = Height - BorderSize.Left - BorderSize.Right;

            var clientRect = ClientArea.GetClientRect(this);

            _nativeWindowRect = new Rectangle(
                (int)((Left + clientRect.Left) * _designUnits.Width),
                (int)((Top + clientRect.Top) * _designUnits.Height),
                (int)(clientRect.Width * _designUnits.Width),
                (int)(clientRect.Height * _designUnits.Height));
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }

        private IntPtr WindowProc(IntPtr windowHandle, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    handled = true;
                    var result = NcHitTest(windowHandle, lParam);
                    return (IntPtr)result;
            }

            return IntPtr.Zero;
        }

        private HitTest NcHitTest(IntPtr windowHandle, IntPtr lParam)
        {
            var window = Window.GetWindow(this) ?? throw new InvalidOperationException("Window needs to be initialized");

            // Arguments are absolute native coordinates
            var hitPoint = new POINT((short)lParam, (short)((uint)lParam >> 16));

            GetWindowRect(windowHandle, out var windowRect);

            var topLeft = windowRect.TopLeft;

            var borderSize = BorderSize;

            if ((window.ResizeMode == ResizeMode.CanResize) || window.ResizeMode == ResizeMode.CanResizeWithGrip)
            {
                if (WindowState.Maximized != window.WindowState)
                {
                    var bottomRight = windowRect.BottomRight;
                    var left = topLeft.X;
                    var top = topLeft.Y;
                    var right = bottomRight.X;
                    var bottom = bottomRight.Y;

                    if ((hitPoint.Y < top) || (hitPoint.Y > bottom) || (hitPoint.X < left) || (hitPoint.X > right))
                        return HitTest.Transparent;

                    if ((hitPoint.Y < (top + borderSize.Top)) && (hitPoint.X < (left + borderSize.Left)))
                        return HitTest.TopLeft;
                    if ((hitPoint.Y < (top + borderSize.Top)) && (hitPoint.X > (right - borderSize.Right)))
                        return HitTest.TopRight;
                    if ((hitPoint.Y > (bottom - borderSize.Bottom)) && (hitPoint.X < (left + borderSize.Left)))
                        return HitTest.BottomLeft;
                    if ((hitPoint.Y > (bottom - borderSize.Bottom)) && (hitPoint.X > (right - borderSize.Right)))
                        return HitTest.BottomRight;
                    if (hitPoint.Y < (top + borderSize.Top))
                        return HitTest.Caption;
                    if (hitPoint.Y > (bottom - borderSize.Bottom))
                        return HitTest.Bottom;
                    if (hitPoint.X < (left + borderSize.Left))
                        return HitTest.Left;
                    if (hitPoint.X > (right - borderSize.Right))
                        return HitTest.Right;
                }
            }

            return HitTest.Client;
        }

        private void Timer_Tick(TimeSpan elapsed)
        {
            if (Interlocked.CompareExchange(ref _timerMutex, 1, 0) != 0)
                return;

            try
            {
                Dispatcher.BeginInvoke(Timer_Tick);
            }
            catch
            {
            }
        }

        private void Timer_Tick()
        {
            try
            {
                var sourceDC = GetDC(_desktopWindowHandle);
                var targetDC = GetWindowDC(_renderTargetHandle);
                var nativeRect = _nativeWindowRect;

                BitBlt(targetDC, 0, 0, nativeRect.Width, nativeRect.Height, sourceDC, nativeRect.Left, nativeRect.Top, CopyPixelOperation.SourceCopy);

                CURSORINFO pci = default;
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                if (GetCursorInfo(ref pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        DrawIcon(targetDC, pci.ptScreenPos.x - (int)Left - 8, pci.ptScreenPos.y - (int)Top - 8, pci.hCursor);
                    }
                }

                ReleaseDC(_renderTargetHandle, targetDC);
                ReleaseDC(_desktopWindowHandle, sourceDC);
            }
            catch
            {
            }
            finally
            {
                Interlocked.Exchange(ref _timerMutex, 0);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private const int WM_NCHITTEST = 0x0084;
        private const Int32 CURSOR_SHOWING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct RECT
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

            public override string ToString()
            {
                return ((Rect)this).ToString();
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct POINT
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

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct SIZE
        {
            public readonly int Width;
            public readonly int Height;

            public SIZE(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public static implicit operator SIZE(System.Windows.Point p)
            {
                return new SIZE((int)Math.Round(p.X), (int)Math.Round(p.Y));
            }

            public static implicit operator System.Windows.Size(SIZE s)
            {
                return new System.Windows.Size(s.Width, s.Height);
            }

            public override string ToString()
            {
                return ((System.Windows.Size)this).ToString();
            }
        }

        private enum HitTest
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
}
