using RegionToShare.Properties;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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

    public ObservableCollection<string> Resolutions { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        Resolutions = new ObservableCollection<string>();
        DataContext = this;

        LoadResolutions();
    }

    private void LoadResolutions() 
    {
        var userDataDirPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\RegionToShare";
        var resolutionsFilePath = userDataDirPath + "\\resolutions.txt";
        
        if(!Directory.Exists(userDataDirPath))
            Directory.CreateDirectory(userDataDirPath);

        if (!File.Exists(resolutionsFilePath))
            File.WriteAllText(resolutionsFilePath, "1024x782\r\n1280x1024\r\n1920x1080");

        string[] lines = File.ReadAllLines(resolutionsFilePath);
        Resolutions = new ObservableCollection<string>(lines);
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
            Top = Top,
            Left = Left,
            Width = 10,
            Height = 10
        };
        separationLayerWindow.SourceInitialized += (_, _) =>
        {
            if (Keyboard.Modifiers != (ModifierKeys.Shift | ModifierKeys.Control))
            {
                var placement = _windowHandle.GetWindowPlacement();
                placement.NormalPosition.DeserializeFrom(Settings.Default.WindowPlacement);
                _windowHandle.SetWindowPlacement(ref placement);
            }

            _separationLayerHandle = separationLayerWindow.GetWindowHandle();
            separationLayerWindow.MouseDown += SubLayer_MouseDown;

            UpdateSizeAndPos();

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

        if (_windowHandle == IntPtr.Zero)
            return;

        if (e.Property != LeftProperty
            && e.Property != TopProperty
            && e.Property != ActualWidthProperty
            && e.Property != ActualHeightProperty)
            return;


        UpdateSizeAndPos();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        Settings.Default.WindowPlacement = _windowHandle.GetWindowPlacement().NormalPosition.Serialize();
        Settings.Default.Save();
    }

    private void UpdateSizeAndPos()
    {
        NativeMethods.GetWindowRect(_windowHandle, out var rect);
        Extend.Text = rect.Width + "x" + rect.Height;

        if (_separationLayerHandle == IntPtr.Zero)
            return;

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
        NativeMethods.SetWindowPos(_separationLayerHandle, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0,
            NativeMethods.SWP_HIDEWINDOW);
        NativeMethods.SetWindowPos(_windowHandle, NativeMethods.HWND_TOP, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE);
    }

    public void SendToBack()
    {
        NativeMethods.SetWindowPos(_separationLayerHandle, NativeMethods.HWND_BOTTOM, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE |
            NativeMethods.SWP_SHOWWINDOW);
        NativeMethods.SetWindowPos(_windowHandle, _separationLayerHandle, 0, 0, 0, 0,
            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }


    private void lbResolutions_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var resolution = e.AddedItems[0].ToString();
        try
        {
            // user can added garbage to txt file
            var wh = resolution.Split('x');
            Width = Convert.ToDouble(wh[0]);
            Height = Convert.ToDouble(wh[1]);

            lbResolutions.SelectedIndex = -1;
        }
        catch (Exception)
        {

        }
    }
}