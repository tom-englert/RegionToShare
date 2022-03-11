using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TomsToolbox.Essentials;
using TomsToolbox.Wpf;
using ButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using Image = System.Windows.Controls.Image;

namespace RegionToShare;

/// <summary>
/// Interaction logic for RecordingWindow.xaml
/// </summary>
public partial class RecordingWindow
{
    public static readonly Thickness BorderSize = new(4, 16, 4, 4);

    private readonly HighResolutionTimer _timer;
    private readonly MainWindow _mainWindow;
    private readonly Image _renderTarget;
    private HwndTarget? _compositionTarget;

    private NativeMethods.RECT _nativeWindowRect;
    private int _timerMutex;

    public RecordingWindow(Image renderTarget, int framesPerSecond = 15)
    {
        InitializeComponent();

        _mainWindow = (MainWindow)GetWindow(renderTarget)!;
        _renderTarget = renderTarget;

        _timer = new HighResolutionTimer(Timer_Tick, TimeSpan.FromSeconds(1.0 / framesPerSecond));
        _timer.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        var hwndSource = (HwndSource?)PresentationSource.FromDependencyObject(this);
        hwndSource?.AddHook(WindowProc);

        _compositionTarget = hwndSource?.CompositionTarget;

        Left = _mainWindow.Left;
        Top = _mainWindow.Top - BorderSize.Top;
        Width = _mainWindow.Width;
        Height = _mainWindow.Height + BorderSize.Top;

        this.BeginInvoke(OnSizeOrPositionChanged);

        base.OnSourceInitialized(e);
    }

    private Transformations DeviceTransformations => _compositionTarget.GetDeviceTransformations();

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

        _mainWindow.Left = Left + BorderSize.Left + MainWindow.DebugOffset.X;
        _mainWindow.Top = Top + BorderSize.Top + MainWindow.DebugOffset.Y;
        _mainWindow.Width = Width - BorderSize.Left - BorderSize.Right;
        _mainWindow.Height = Height - BorderSize.Top - BorderSize.Bottom;

        var clientRect = ClientArea.GetClientRect(this);

        var screenOffset = new Vector(Left, Top);

        var toDevice = DeviceTransformations.ToDevice;
        var topLeft = toDevice.Transform(clientRect.TopLeft + screenOffset);
        var bottomRight = toDevice.Transform(clientRect.BottomRight + screenOffset);

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

        var transformations = DeviceTransformations;

        var borderSize = transformations.ToDevice.Transform(BorderSize);

        var clientPoint = transformations.FromDevice.Transform(hitPoint - topLeft);

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
            var nativeRect = _nativeWindowRect;

            using var bitmap = new Bitmap(nativeRect.Width, nativeRect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(nativeRect.Left, nativeRect.Top, 0, 0, new System.Drawing.Size(nativeRect.Width, nativeRect.Height));
            var bitmapHandle = bitmap.GetHbitmap();
            var imageSource = Imaging.CreateBitmapSourceFromHBitmap(bitmapHandle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            NativeMethods.DeleteObject(bitmapHandle);

            _renderTarget.Source = imageSource;
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