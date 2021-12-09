using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TomsToolbox.Essentials;
using TomsToolbox.Wpf;

namespace RegionToShare;

/// <summary>
/// Interaction logic for RecordingWindow.xaml
/// </summary>
public partial class RecordingWindow
{
    public static readonly Thickness BorderSize = new(4, 16, 4, 4);

    private readonly HighResolutionTimer _timer;
    private readonly Window _mainWindow;
    private readonly IntPtr _mainWindowHandle;
    private readonly IntPtr _renderTargetHandle;
    private readonly IntPtr _desktopWindowHandle;
    private readonly Matrix _transformFromDevice;
    private readonly Matrix _transformToDevice;

    private NativeMethods.RECT _nativeWindowRect;
    private int _timerMutex;

    public RecordingWindow(Window mainWindow, IntPtr renderTargetHandle, int framesPerSecond = 15)
    {
        InitializeComponent();

        _mainWindow = mainWindow;
        _mainWindowHandle = mainWindow.GetWindowHandle();
        _renderTargetHandle = renderTargetHandle;
        _desktopWindowHandle = NativeMethods.GetDesktopWindow();

        _timer = new HighResolutionTimer(Timer_Tick, TimeSpan.FromSeconds(1.0 / framesPerSecond));
        _timer.Start();

        var compositionTarget = ((HwndSource)PresentationSource.FromDependencyObject(mainWindow)).CompositionTarget;
        _transformFromDevice = compositionTarget.TransformFromDevice;
        _transformToDevice = compositionTarget.TransformToDevice;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        var messageSource = (HwndSource)PresentationSource.FromDependencyObject(this);
        messageSource.AddHook(WindowProc);

        Left = _mainWindow.Left;
        Top = _mainWindow.Top - BorderSize.Top;
        Width = _mainWindow.Width;
        Height = _mainWindow.Height + BorderSize.Top;

        this.BeginInvoke(OnSizeOrPositionChanged);

        base.OnSourceInitialized(e);
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

        if (e.Property != LeftProperty
            && e.Property != TopProperty
            && e.Property != ActualWidthProperty
            && e.Property != ActualHeightProperty)
            return;

        OnSizeOrPositionChanged();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        _mainWindow.Left = Left;
        _mainWindow.Top = Top + BorderSize.Top;
        _mainWindow.Width = Width;
        _mainWindow.Height = Height - BorderSize.Top;

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
            case NativeMethods.WM_NCHITTEST:
                handled = true;
                return (IntPtr)NcHitTest(windowHandle, lParam);
        }

        return IntPtr.Zero;
    }

    private NativeMethods.HitTest NcHitTest(IntPtr windowHandle, IntPtr lParam)
    {
        if (WindowState.Normal != WindowState)
            return NativeMethods.HitTest.Client;

        if ((ResizeMode != ResizeMode.CanResize) && ResizeMode != ResizeMode.CanResizeWithGrip)
            return NativeMethods.HitTest.Client;

        // Arguments are absolute native coordinates
        var hitPoint = new NativeMethods.POINT((short)lParam, (short)((uint)lParam >> 16));

        NativeMethods.GetWindowRect(windowHandle, out var windowRect);

        var topLeft = windowRect.TopLeft;
        var bottomRight = windowRect.BottomRight;

        var borderSize = _transformToDevice.Transform(BorderSize);

        var clientPoint = _transformFromDevice.Transform(hitPoint - topLeft);

        if (InputHitTest(clientPoint) is FrameworkElement element)
        {
            if (element.AncestorsAndSelf().OfType<ButtonBase>().Any())
            {
                return NativeMethods.HitTest.Client;
            }
        }

        var left = topLeft.X;
        var top = topLeft.Y;
        var right = bottomRight.X;
        var bottom = bottomRight.Y;

        if ((hitPoint.Y < top) || (hitPoint.Y > bottom) || (hitPoint.X < left) || (hitPoint.X > right))
            return NativeMethods.HitTest.Transparent;

        if ((hitPoint.Y < (top + borderSize.Top)) && (hitPoint.X < (left + borderSize.Left)))
            return NativeMethods.HitTest.TopLeft;
        if ((hitPoint.Y < (top + borderSize.Top)) && (hitPoint.X > (right - borderSize.Right)))
            return NativeMethods.HitTest.TopRight;
        if ((hitPoint.Y > (bottom - borderSize.Bottom)) && (hitPoint.X < (left + borderSize.Left)))
            return NativeMethods.HitTest.BottomLeft;
        if ((hitPoint.Y > (bottom - borderSize.Bottom)) && (hitPoint.X > (right - borderSize.Right)))
            return NativeMethods.HitTest.BottomRight;
        if (hitPoint.Y < (top + borderSize.Top))
            return NativeMethods.HitTest.Caption;
        if (hitPoint.Y > (bottom - borderSize.Bottom))
            return NativeMethods.HitTest.Bottom;
        if (hitPoint.X < (left + borderSize.Left))
            return NativeMethods.HitTest.Left;
        if (hitPoint.X > (right - borderSize.Right))
            return NativeMethods.HitTest.Right;

        return NativeMethods.HitTest.Client;
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
            var sourceDC = NativeMethods.GetDC(_desktopWindowHandle);
            var targetDC = NativeMethods.GetWindowDC(_renderTargetHandle);
            var nativeRect = _nativeWindowRect;

            // SetWindowDisplayAffinity(_mainWindowHandle, 0x11);
            NativeMethods.BitBlt(targetDC, 0, 0, nativeRect.Width, nativeRect.Height, sourceDC, nativeRect.Left, nativeRect.Top, CopyPixelOperation.SourceCopy);
            // SetWindowDisplayAffinity(_mainWindowHandle, 0);

            NativeMethods.CURSORINFO pci = default;
            pci.cbSize = Marshal.SizeOf(typeof(NativeMethods.CURSORINFO));

            if (NativeMethods.GetCursorInfo(ref pci))
            {
                if (pci.flags == NativeMethods.CURSOR_SHOWING)
                {
                    NativeMethods.DrawIcon(targetDC, pci.ptScreenPos.X - (int)Left - 4, pci.ptScreenPos.Y - (int)Top - 16, pci.hCursor);
                }
            }

            NativeMethods.ReleaseDC(_renderTargetHandle, targetDC);
            NativeMethods.ReleaseDC(_desktopWindowHandle, sourceDC);
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