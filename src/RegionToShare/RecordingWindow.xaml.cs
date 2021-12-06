using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TomsToolbox.Essentials;
using TomsToolbox.Wpf;

namespace RegionToShare
{
    /// <summary>
    /// Interaction logic for RecordingWindow.xaml
    /// </summary>
    public partial class RecordingWindow : Window
    {
        public static readonly Thickness BorderSize = new(4, 16, 4, 4);

        private readonly HighResolutionTimer _timer;
        private readonly Window _mainWindow;
        private readonly IntPtr _renderTargetHandle;
        private readonly IntPtr _desktopWindowHandle;
        private readonly Matrix _transformFromDevice;
        private readonly Matrix _transformToDevice;

        private RECT _nativeWindowRect;
        private int _timerMutex;

        public RecordingWindow(Window mainWindow, IntPtr renderTargetHandle,
            int framesPerSecond = 15)
        {
            InitializeComponent();

            _mainWindow = mainWindow;
            _renderTargetHandle = renderTargetHandle;
            _desktopWindowHandle = GetDesktopWindow();

            _timer = new(Timer_Tick, TimeSpan.FromSeconds(1.0 / framesPerSecond));
            _timer.Start();

            var compositionTarget = ((HwndSource)PresentationSource.FromDependencyObject(mainWindow)).CompositionTarget;
            _transformFromDevice = compositionTarget.TransformFromDevice;
            _transformToDevice = compositionTarget.TransformToDevice;

            Left = mainWindow.Left - BorderSize.Left;
            Top = mainWindow.Top - BorderSize.Top;
            Width = mainWindow.Width + BorderSize.Left + BorderSize.Right;
            Height = mainWindow.Height + BorderSize.Top + BorderSize.Bottom;

            // Workaround to get the background initialized properly
            Top += 1;
            this.BeginInvoke(() => Top -= 1);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var messageSource = (HwndSource)PresentationSource.FromDependencyObject(this);
            messageSource.AddHook(WindowProc);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            OnSizeOrPositionChanged();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == WindowStateProperty)
            {
                this.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    WindowState = WindowState.Normal;
                    OnSizeOrPositionChanged();
                });

                return;
            }

            if (e.Property != LeftProperty && e.Property != TopProperty)
                return;

            OnSizeOrPositionChanged();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _timer.Stop();
        }

        private void OnSizeOrPositionChanged()
        {
            if (!IsLoaded)
                return;

            _mainWindow.Left = Left + BorderSize.Left;
            _mainWindow.Top = Top + BorderSize.Top;
            _mainWindow.Width = Width - BorderSize.Left - BorderSize.Right;
            _mainWindow.Height = Height - BorderSize.Top - BorderSize.Bottom;

            var clientRect = ClientArea.GetClientRect(this);

            var screenOffset = new Vector(Left, Top);
            var topLeft = _transformToDevice.Transform(clientRect.TopLeft + screenOffset);
            var bottomRight = _transformToDevice.Transform(clientRect.BottomRight + screenOffset);

            _nativeWindowRect = new Rect(topLeft, bottomRight);
        }

        private IntPtr WindowProc(IntPtr windowHandle, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    handled = true;
                    return (IntPtr)NcHitTest(windowHandle, lParam);
            }

            return IntPtr.Zero;
        }

        private HitTest NcHitTest(IntPtr windowHandle, IntPtr lParam)
        {
            if (WindowState.Normal != WindowState)
                return HitTest.Client;

            if ((ResizeMode != ResizeMode.CanResize) && ResizeMode != ResizeMode.CanResizeWithGrip)
                return HitTest.Client;

            // Arguments are absolute native coordinates
            var hitPoint = new POINT((short)lParam, (short)((uint)lParam >> 16));

            GetWindowRect(windowHandle, out var windowRect);

            var topLeft = windowRect.TopLeft;
            var bottomRight = windowRect.BottomRight;

            var borderSize = _transformToDevice.Transform(BorderSize);

            var clientPoint = _transformFromDevice.Transform(hitPoint - topLeft);

            if (InputHitTest(clientPoint) is FrameworkElement element)
            {
                if (element.AncestorsAndSelf().OfType<ButtonBase>().Any())
                {
                    return HitTest.Client;
                }
            }

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
                // Window already unloaded
            }
        }

        private void Timer_Tick()
        {
            try
            {
                var sourceDC = GetDC(_desktopWindowHandle);
                var targetDC = GetWindowDC(_renderTargetHandle);
                var nativeRect = _nativeWindowRect;

                BitBlt(targetDC, 0, 0, nativeRect.Width, nativeRect.Height, IntPtr.Zero, 0, 0, CopyPixelOperation.Blackness);
                BitBlt(targetDC, 0, 0, nativeRect.Width, nativeRect.Height, sourceDC, nativeRect.Left, nativeRect.Top, CopyPixelOperation.SourceCopy);

                CURSORINFO pci = default;
                pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                if (GetCursorInfo(ref pci))
                {
                    if (pci.flags == CURSOR_SHOWING)
                    {
                        DrawIcon(targetDC, pci.ptScreenPos.x - (int)Left - 4, pci.ptScreenPos.y - (int)Top - 16, pci.hCursor);
                    }
                }

                ReleaseDC(_renderTargetHandle, targetDC);
                ReleaseDC(_desktopWindowHandle, sourceDC);
            }
            catch
            {
                // Window already unloaded
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

        // ReSharper disable all

        private const int WM_NCHITTEST = 0x0084;
        private const int CURSOR_SHOWING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
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

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

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

    internal static class ExtensionMethods
    {
        public static Thickness Transform(this Matrix matrix, Thickness value)
        {
            var topLeft = matrix.Transform(new Vector(value.Left, value.Top));
            var bottomRight = matrix.Transform(new Vector(value.Right, value.Bottom));

            return new Thickness(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
        }
    }
}
