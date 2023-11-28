using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RegionToShare.Properties;
using Throttle;
using TomsToolbox.Wpf;
using TomsToolbox.Wpf.Styles;
using static RegionToShare.NativeMethods;
using static RegionToShare.ExtensionMethods;

namespace RegionToShare;

public partial class MainWindow
{
    private IntPtr _separationLayerHandle;

    private IntPtr _windowHandle;
    private RecordingWindow? _recordingWindow;

    private POINT _debugOffset;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        Resolutions = LoadResolutions();
        Resources.RegisterDefaultStyles();
        ValidateSettings();
        SetThemeColor();
        Settings.PropertyChanged += Settings_PropertyChanged;
    }

    public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public ICollection<string> Resolutions { get; }

    public ICollection<int> FramesPerSecondSource { get; } = new[] { 5, 10, 15, 20, 30, 60 };

    internal Settings Settings => Settings.Default;

    public string? Extend
    {
        get => (string?)GetValue(ExtendProperty);
        set => SetValue(ExtendProperty, value);
    }
    public static readonly DependencyProperty ExtendProperty = DependencyProperty.Register(nameof(Extend), typeof(string), typeof(MainWindow),
        new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, args) => ((MainWindow)d).OnExtendChanged(args.NewValue as string)));

    public Brush BackgroundPattern
    {
        get => (Brush)GetValue(BackgroundPatternProperty);
        set => SetValue(BackgroundPatternProperty, value);
    }
    public static readonly DependencyProperty BackgroundPatternProperty = DependencyProperty.Register(
        nameof(BackgroundPattern), typeof(Brush), typeof(MainWindow), new PropertyMetadata(default(Brush)));

    private void OnExtendChanged(string? newValue)
    {
        if (newValue is null || !TryParseSize(newValue, out var size))
            return;

        size += GlassFrameThickness;
        SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0, size.Width, size.Height, SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOMOVE);
    }

    internal Thickness GlassFrameThickness => DwmGetExtendedFrameBounds(_windowHandle);

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
                File.WriteAllLines(resolutionsFilePath, defaultResolutions);
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
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            Title = "Region to Share - Separation Layer",
            ShowInTaskbar = false,
            Top = Top,
            Left = Left,
            Width = 10,
            Height = 10
        };

        separationLayerWindow.MouseDown += SubLayer_MouseDown;
        BindingOperations.SetBinding(separationLayerWindow, BackgroundProperty, new Binding(nameof(BackgroundPattern)) { Source = this });

        separationLayerWindow.SourceInitialized += (_, _) =>
        {
            _separationLayerHandle = separationLayerWindow.GetWindowHandle();

            this.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
            {
                if (Keyboard.Modifiers != (ModifierKeys.Alt | ModifierKeys.Control))
                {
                    var placement = _windowHandle.GetWindowPlacement();
                    placement.NormalPosition.DeserializeFrom(Settings.WindowPlacement);
                    _windowHandle.SetWindowPlacement(ref placement);
                }

                UpdateSizeAndPos();

                if (Settings.StartActivated)
                {
                    SetActive();
                }
                else
                {
                    this.BeginInvoke(BringToFront);
                }
            });
        };

        separationLayerWindow.Show();
    }

    private void SetActive()
    {
        OnMouseLeftButtonDown();

        var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle, Dispatcher.CurrentDispatcher);

        void TimerTick(object sender, EventArgs e)
        {
            if (_recordingWindow != null)
            {
                SendToBack();
            }
            timer.Stop();
        }

        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += TimerTick;
        timer.Start();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        OnMouseLeftButtonDown();
    }

    private void OnMouseLeftButtonDown()
    {
        _debugOffset = Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Control | ModifierKeys.Shift) ? new POINT(600, 300) : new POINT();

        if (_recordingWindow != null)
            return;

        InfoArea.Visibility = Visibility.Collapsed;
        RenderTarget.Visibility = Visibility.Visible;

        ValidateSettings();

        _recordingWindow = new RecordingWindow(RenderTarget, Settings.DrawShadowCursor, Settings.FramesPerSecond, _debugOffset);

        NativeWindowRect -= GlassFrameThickness;

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

            NativeWindowRect += GlassFrameThickness;

            BringToFront();
        };

        _recordingWindow.Show();

        this.BeginInvoke(DispatcherPriority.Background, SendToBack);
    }

    private void ValidateSettings()
    {
        Settings.FramesPerSecond = FramesPerSecondSource.Contains(Settings.FramesPerSecond) ? Settings.FramesPerSecond : 15;
        try
        {
            ColorConverter.ConvertFromString(Settings.ThemeColor);
        }
        catch
        {
            Settings.ThemeColor = nameof(Colors.SteelBlue);
        }
    }

    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.ThemeColor))
        {
            SetThemeColor();
        }
    }

    private void SetThemeColor()
    {
        try
        {
            var themeColor = (Color)ColorConverter.ConvertFromString(Settings.ThemeColor);
            Application.Current.Resources["ThemeColor"] = themeColor;
            BackgroundPattern = GenerateRandomBrush(themeColor);
        }
        catch
        {
            // Invalid color, ignore.
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_windowHandle == IntPtr.Zero)
            return;

        if (e.Property != LeftProperty
            && e.Property != TopProperty
            && e.Property != ActualWidthProperty
            && e.Property != ActualHeightProperty
            && e.Property != WindowStateProperty)
            return;

        UpdateSizeAndPos();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        Settings.WindowPlacement = _windowHandle.GetWindowPlacement().NormalPosition.Serialize();
        Settings.Save();
    }

    [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.Normal)]
    private void UpdateSizeAndPos()
    {
        if (WindowState == WindowState.Minimized)
            return;

        _recordingWindow?.UpdateSizeAndPos(NativeWindowRect);

        var rect = NativeWindowRect - GlassFrameThickness;
        Extend = rect.Width + "x" + rect.Height;

        SetSeparationLayerPos(SWP_NOACTIVATE | SWP_NOZORDER);
    }

    private void SubLayer_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.BeginInvoke(DispatcherPriority.Background, SendToBack);
    }

    public void BringToFront()
    {
        SetSeparationLayerPos(SWP_HIDEWINDOW);
        SetWindowPos(_windowHandle, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    public void SendToBack()
    {
        SetSeparationLayerPos(SWP_NOACTIVATE | SWP_SHOWWINDOW);
        SetWindowPos(_windowHandle, _separationLayerHandle, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    private void SetSeparationLayerPos(uint flags)
    {
        if (_separationLayerHandle == IntPtr.Zero)
            return;

        var rect = NativeWindowRect - _debugOffset;

        SetWindowPos(_separationLayerHandle, HWND_BOTTOM, rect.Left, rect.Top, rect.Width, rect.Height, flags);
    }

    private bool TryParseSize(string value, out SIZE size)
    {
        size = Size.Empty;

        try
        {
            var parts = value.Split('x');

            if (parts.Length != 2
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
                return false;

            size = new SIZE(width, height);

            return size.Width >= MinWidth && size.Height >= MinHeight;
        }
        catch
        {
            return false;
        }
    }
}