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

    internal static readonly NativeMethods.POINT DebugOffset = new(0, 0);

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = this.GetWindowHandle();

        var separationLayerWindow = new Window()
        {
            Background = (Brush)FindResource("HatchBrush"),
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            Title = "Region to Share - Separation Layer",
            ShowInTaskbar = false,
            Top = this.Top,
            Left = this.Left,
            Width = 10,
            Height = 10
        };
        separationLayerWindow.SourceInitialized += (_, _) =>
        {
            _separationLayerHandle = separationLayerWindow.GetWindowHandle();
            separationLayerWindow.MouseDown += SubLayer_MouseDown;

            this.BeginInvoke(BringToFront);
        };

        separationLayerWindow.Show();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_recordingWindow != null)
            return;

        InfoArea.Visibility = Visibility.Collapsed;
        RenderTarget.Visibility = Visibility.Visible;

        _recordingWindow = new RecordingWindow(RenderTarget);

        _recordingWindow.SourceInitialized += (_, _) =>
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
        };

        _recordingWindow.Closed += (_, _) =>
        {
            InfoArea.Visibility = Visibility.Visible;
            RenderTarget.Visibility = Visibility.Hidden;
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
        NativeMethods.SetWindowPos(_separationLayerHandle, IntPtr.Zero,
            rect.Left - (DebugOffset.X / 2), rect.Top - (DebugOffset.Y / 2),
            rect.Width, rect.Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER);
    }

    private void SubLayer_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.BeginInvoke(DispatcherPriority.Background, SendToBack);
    }

    public void BringToFront()
    {
        NativeMethods.SetWindowPos(_separationLayerHandle, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0, NativeMethods.SWP_HIDEWINDOW);
        NativeMethods.SetWindowPos(_windowHandle, NativeMethods.HWND_TOP, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
    }

    public void SendToBack()
    {
        NativeMethods.SetWindowPos(_separationLayerHandle, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        NativeMethods.SetWindowPos(_windowHandle, _separationLayerHandle, 0, 0, 0, 0, NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }
}