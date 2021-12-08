using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TomsToolbox.Wpf;

namespace RegionToShare;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
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
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_recordingWindow != null)
            return;

        SendToBack();

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
    }

    private void RenderTargetWindow_OnMouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        this.BeginInvoke(DispatcherPriority.Background, SendToBack);
    }

    private void BringToFront()
    {
        SetWindowPos(_windowHandle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    private void SendToBack()
    {
        SetWindowPos(_windowHandle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    // ReSharper disable InconsistentNaming
    // ReSharper disable IdentifierTypo
    private static readonly IntPtr HWND_TOP = new(0);
    private static readonly IntPtr HWND_BOTTOM = new(1);
    private const int SWP_NOSIZE = 0x0001;
    private const int SWP_NOMOVE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}