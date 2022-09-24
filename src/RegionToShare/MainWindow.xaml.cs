using RegionToShare.Properties;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TomsToolbox.Wpf;
using static RegionToShare.NativeMethods;

namespace RegionToShare;

public partial class MainWindow
{
    private IntPtr _separationLayerHandle;

    private IntPtr _windowHandle;
    private RecordingWindow? _recordingWindow;

    internal static readonly POINT DebugOffset = new(0, 0);

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Resolutions = LoadResolutions();
    }

    public ICollection<string> Resolutions { get; }

    public string? Extend
    {
        get => (string?)GetValue(ExtendProperty);
        set => SetValue(ExtendProperty, value);
    }
    public static readonly DependencyProperty ExtendProperty = DependencyProperty.Register("Extend", typeof(string), typeof(MainWindow),
        new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, args) => ((MainWindow)d).OnExtendChanged((string)args.NewValue)));

    private void OnExtendChanged(string newValue)
    {
        if (TryParseSize(newValue, out var size))
        {
            SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0, (int)size.Width, (int)size.Height, SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOMOVE);
        }
    }

    internal RECT NativeWindowRect
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

    private ICollection<string> LoadResolutions()
    {
        var defaultResolutions = new[] { @"1024x782", @"1280x1024", @"1920x1080" };

        try
        {
            var userDataDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RegionToShare");
            var resolutionsFilePath = Path.Combine(userDataDirPath, @"resolutions.txt");

            Directory.CreateDirectory(userDataDirPath);

            if (!File.Exists(resolutionsFilePath))
            {
                File.WriteAllText(resolutionsFilePath, string.Join("\r\n", defaultResolutions));
                return defaultResolutions;
            }

            var resolutions = File.ReadAllLines(resolutionsFilePath)
                .Where(item => TryParseSize(item, out _))
                .ToArray();

            return resolutions.Any() ? resolutions : defaultResolutions;
        }
        catch
        {
            return defaultResolutions;
        }
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
        var rect = NativeWindowRect;

        Extend = rect.Width + "x" + rect.Height;

        if (_separationLayerHandle == IntPtr.Zero)
            return;

        SetWindowPos(_separationLayerHandle, IntPtr.Zero, rect.Left - DebugOffset.X, rect.Top - DebugOffset.Y, rect.Width, rect.Height, SWP_NOACTIVATE | SWP_NOZORDER);
    }

    private void SubLayer_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.BeginInvoke(DispatcherPriority.Background, SendToBack);
    }

    public void BringToFront()
    {
        SetWindowPos(_separationLayerHandle, HWND_BOTTOM, 0, 0, 0, 0, SWP_HIDEWINDOW);
        SetWindowPos(_windowHandle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    public void SendToBack()
    {
        SetWindowPos(_separationLayerHandle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        SetWindowPos(_windowHandle, _separationLayerHandle, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    private bool TryParseSize(string value, out Size size)
    {
        size = Size.Empty;

        try
        {
            var parts = value.Split('x');
            if (parts.Length != 2)
                return false;

            size = new Size(double.Parse(parts[0]), double.Parse(parts[1]));

            return size.Width >= MinWidth && size.Height >= MinHeight;
        }
        catch
        {
            return false;
        }
    }
}