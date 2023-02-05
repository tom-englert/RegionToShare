using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TomsToolbox.Essentials;
using TomsToolbox.Wpf;
using static RegionToShare.NativeMethods;
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

    private RECT _nativeWindowRect;
    private int _timerMutex;
    private IntPtr _windowHandle;

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
        if (hwndSource == null)
            return;

        hwndSource.AddHook(WindowProc);

        _compositionTarget = hwndSource.CompositionTarget;
        _windowHandle = hwndSource.Handle;

        var rect = _mainWindow.NativeWindowRect + NativeBorderSize;

        NativeWindowRect = rect;

        this.BeginInvoke(OnSizeOrPositionChanged);

        base.OnSourceInitialized(e);
    }

    private Transformations DeviceTransformations => _compositionTarget.GetDeviceTransformations();

    private RECT NativeWindowRect
    {
        get
        {
            GetWindowRect(_windowHandle, out var rect);
            return rect;
        }
        set
        {
            if (_windowHandle == IntPtr.Zero)
                return;
            
            SetWindowPos(_windowHandle, IntPtr.Zero, value.Left, value.Top, value.Width, value.Height, SWP_NOACTIVATE | SWP_NOZORDER);
        }
    }

    private Thickness NativeBorderSize => DeviceTransformations.ToDevice.Transform(BorderSize);

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

        _timer.Stop();
    }

    private void OnSizeOrPositionChanged()
    {
        if (!IsLoaded)
            return;

        _mainWindow.NativeWindowRect = _nativeWindowRect = NativeWindowRect - NativeBorderSize;
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

        var transformations = DeviceTransformations;

        var borderSize = transformations.ToDevice.Transform(BorderSize);

        var clientPoint = transformations.FromDevice.Transform(hitPoint - topLeft);

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
            var nativeRect = _nativeWindowRect;

            using var bitmap = new Bitmap(nativeRect.Width, nativeRect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(nativeRect.Left, nativeRect.Top, 0, 0, new System.Drawing.Size(nativeRect.Width, nativeRect.Height));
            var bitmapHandle = bitmap.GetHbitmap();
            var imageSource = Imaging.CreateBitmapSourceFromHBitmap(bitmapHandle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(bitmapHandle);

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