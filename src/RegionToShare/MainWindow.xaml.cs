using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TomsToolbox.Wpf;

namespace RegionToShare;

public partial class MainWindow
{
    private IntPtr _separationLayerHandle;

    private IntPtr _windowHandle;
    private RecordingWindow? _recordingWindow;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = this.GetWindowHandle();

        var separationLayerWindow = new Window() { Background = (Brush)FindResource("HatchBrush"), WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, Title = "Region to Share - Separation Layer", ShowInTaskbar = false };
        separationLayerWindow.SourceInitialized += (sender, args) =>
        {
            _separationLayerHandle = separationLayerWindow.GetWindowHandle();
            separationLayerWindow.MouseLeftButtonDown += SeparationLayerWindow_MouseLeftButtonDown;
        };
        separationLayerWindow.Show();

        BringToFront();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_recordingWindow != null)
            return;

        InfoArea.Visibility = Visibility.Collapsed;
        RenderTargetHost.Visibility = Visibility.Visible;

        _recordingWindow = new RecordingWindow(this, RenderTargetWindow.Handle);

        _recordingWindow.SourceInitialized += (_, _) =>
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
        };

        _recordingWindow.Closed += (_, _) =>
        {
            InfoArea.Visibility = Visibility.Visible;
            RenderTargetHost.Visibility = Visibility.Collapsed;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            ResizeMode = ResizeMode.CanResize;

            _recordingWindow = null;

            BringToFront();
        };

        _recordingWindow.Show();
        this.BeginInvoke(DispatcherPriority.Background, SendToBack);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property != LeftProperty
            && e.Property != TopProperty
            && e.Property != ActualWidthProperty
            && e.Property != ActualHeightProperty)
            return;

        NativeMethods.GetWindowRect(_windowHandle, out var rect);
        NativeMethods.SetWindowPos(_separationLayerHandle, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height, NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER);
    }

    private void SeparationLayerWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        this.BeginInvoke(DispatcherPriority.Background, SendToBack);
    }

    private void RenderTargetWindow_OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        this.BeginInvoke(DispatcherPriority.Background, SendToBack);
    }

    private void BringToFront()
    {
        NativeMethods.SetWindowPos(_separationLayerHandle, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0, NativeMethods.SWP_HIDEWINDOW);
        NativeMethods.SetWindowPos(_windowHandle, NativeMethods.HWND_TOP, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
    }

    private void SendToBack()
    {
        NativeMethods.SetWindowPos(_separationLayerHandle, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        NativeMethods.SetWindowPos(_windowHandle, _separationLayerHandle, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }
}