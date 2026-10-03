using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using RegionToShare.Properties;
using Throttle;
using TomsToolbox.Essentials;
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

    private const int FitWindowHotkeyId = 1;

    private POINT _debugOffset;
    private bool _isUpdatingExtend;
    private bool _areHotkeysSuspended;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = this;
        Resolutions = LoadResolutions();
        AspectRatios = LoadAspectRatios();
        AspectRatio = AspectRatios.FirstOrDefault(item => item.Name == Settings.AspectRatio) ?? AspectRatio.Free;
        Resources.RegisterDefaultStyles();
        SetThemeColor();
        Settings.PropertyChanged += Settings_PropertyChanged;
    }

    public string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    public ICollection<string> Resolutions { get; }

    public ICollection<AspectRatio> AspectRatios { get; }

    public static ICollection<int> SupportedFramesPerSecond { get; } = new[] { 5, 10, 15, 20, 30, 60 };

    internal Settings Settings => Settings.Default;

    internal MouseHighlighter MouseHighlighter { get; } = new();

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

    public AspectRatio AspectRatio
    {
        get => (AspectRatio)GetValue(AspectRatioProperty);
        set => SetValue(AspectRatioProperty, value);
    }
    public static readonly DependencyProperty AspectRatioProperty = DependencyProperty.Register(nameof(AspectRatio), typeof(AspectRatio), typeof(MainWindow),
        new FrameworkPropertyMetadata(AspectRatio.Free, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, args) => ((MainWindow)d).OnAspectRatioChanged((AspectRatio?)args.NewValue ?? AspectRatio.Free)));

    public bool IsFitWindowHotkeyInUse
    {
        get => (bool)GetValue(IsFitWindowHotkeyInUseProperty);
        set => SetValue(IsFitWindowHotkeyInUseProperty, value);
    }
    public static readonly DependencyProperty IsFitWindowHotkeyInUseProperty = DependencyProperty.Register(
        nameof(IsFitWindowHotkeyInUse), typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

    internal bool AreHotkeysSuspended
    {
        get => _areHotkeysSuspended;
        set
        {
            if (_areHotkeysSuspended == value)
                return;

            _areHotkeysSuspended = value;
            RegisterHotkeys();
        }
    }

    private void RegisterHotkeys()
    {
        if (_windowHandle == IntPtr.Zero)
            return;

        UnregisterHotKey(_windowHandle, FitWindowHotkeyId);

        if (_areHotkeysSuspended)
            return;

        var hotkey = Hotkey.Parse(Settings.FitWindowHotkey);

        IsFitWindowHotkeyInUse = !hotkey.IsNone
                                 && !RegisterHotKey(_windowHandle, FitWindowHotkeyId, hotkey.NativeModifiers | MOD_NOREPEAT, hotkey.NativeKey);
    }

    private void OnExtendChanged(string? newValue)
    {
        if (_isUpdatingExtend || newValue is null || !TryParseSize(newValue, out var size))
            return;

        if (!AspectRatio.IsFree)
        {
            size.Height = AspectRatio.HeightFromWidth(size.Width);
        }

        SetRegionSize(size);
    }

    private void OnAspectRatioChanged(AspectRatio aspectRatio)
    {
        Settings.AspectRatio = aspectRatio.Name;

        if (aspectRatio.IsFree || _windowHandle == IntPtr.Zero)
            return;

        var region = NativeWindowRect - GlassFrameThickness;

        SetRegionSize(new SIZE(region.Width, aspectRatio.HeightFromWidth(region.Width)));
    }

    private void SetRegionSize(SIZE size)
    {
        if (_windowHandle == IntPtr.Zero)
            return;

        size += GlassFrameThickness;
        SetWindowPos(_windowHandle, IntPtr.Zero, 0, 0, size.Width, size.Height, SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOMOVE);

        // Refresh the displayed extend, even if the window size did not change.
        UpdateSizeAndPos();
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

        return LoadUserList(@"resolutions.txt", defaultResolutions, item => TryParseSize(item, out _));
    }

    private static ICollection<AspectRatio> LoadAspectRatios()
    {
        var defaultAspectRatios = new[] { @"4:3", @"5:4", @"16:10", @"16:9" };

        var aspectRatios = LoadUserList(@"aspectratios.txt", defaultAspectRatios, item => AspectRatio.TryParse(item, out _))
            .Select(item => AspectRatio.TryParse(item, out var aspectRatio) ? aspectRatio : AspectRatio.Free)
            .Where(item => !item.IsFree);

        return new[] { AspectRatio.Free }.Concat(aspectRatios).ToArray();
    }

    private static ICollection<string> LoadUserList(string fileName, ICollection<string> defaultItems, Func<string, bool> isValid)
    {
        try
        {
            var userDataDirPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"RegionToShare");
            var filePath = Path.Combine(userDataDirPath, fileName);

            Directory.CreateDirectory(userDataDirPath);

            if (!File.Exists(filePath))
            {
                File.WriteAllLines(filePath, defaultItems);
                return defaultItems;
            }

            var items = File.ReadAllLines(filePath)
                .Where(isValid)
                .ToArray();

            return items.Any() ? items : defaultItems;
        }
        catch
        {
            return defaultItems;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _windowHandle = this.GetWindowHandle();

        HwndSource.FromHwnd(_windowHandle)?.AddHook(WindowProc);

        RegisterHotkeys();

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

                    placement.NormalPosition += GlassFrameThickness;
                    _windowHandle.SetWindowPlacement(ref placement);
                    // need to set it twice, if the first call has moved the window to another screen with a different dpi, the size might be incorrect.
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

            ShowInTaskbar = true;
            BringToFront();
        };

        _recordingWindow.Show();

        this.BeginInvoke(DispatcherPriority.Background, SendToBack);
    }

    public static bool ValidateSettings()
    {
        try
        {
            var settings = Settings.Default;

            settings.FramesPerSecond = SupportedFramesPerSecond.Contains(settings.FramesPerSecond) ? settings.FramesPerSecond : 15;

            // Colors are shown in hex format, also convert color names from older versions.
            settings.ThemeColor = NormalizeColor(settings.ThemeColor, Colors.SteelBlue);
            settings.HighlighterColor = NormalizeColor(settings.HighlighterColor, Colors.Yellow);
            settings.HighlighterLeftClickColor = NormalizeColor(settings.HighlighterLeftClickColor, Colors.Red);
            settings.HighlighterRightClickColor = NormalizeColor(settings.HighlighterRightClickColor, Colors.Blue);

            return true;
        }
        catch (ConfigurationException ex)
        {
            var inner = ex.ExceptionChain().OfType<ConfigurationException>().FirstOrDefault(item => !item.Filename.IsNullOrEmpty());
            if (inner == null)
                throw;

            var message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.Error_CorruptSettings, inner.Filename);
            MessageBox.Show(message, Properties.Resources.Error_Title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.ServiceNotification);
            File.Delete(inner.Filename);
        }

        return false;
    }

    private static string NormalizeColor(string? value, Color fallback)
    {
        return ColorBrushConverter.ToHex(ColorBrushConverter.TryParseColor(value, out var color) ? color : fallback);
    }

    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.ThemeColor):
                SetThemeColor();
                break;

            case nameof(Settings.FitWindowHotkey):
                RegisterHotkeys();
                break;
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }

    internal void OpenSettings()
    {
        SettingsWindow.Open(this);
    }

    private void FitForegroundWindow()
    {
        WindowFitter.FitForegroundWindow(NativeWindowRect - GlassFrameThickness);
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

        UnregisterHotKey(_windowHandle, FitWindowHotkeyId);
        MouseHighlighter.Dispose();

        var normalPosition = _windowHandle.GetWindowPlacement().NormalPosition - GlassFrameThickness;
        Settings.WindowPlacement = normalPosition.Serialize();
        Settings.Save();
    }

    [Throttled(typeof(DispatcherThrottle), (int)DispatcherPriority.Normal)]
    private void UpdateSizeAndPos()
    {
        if (WindowState == WindowState.Minimized)
            return;

        _recordingWindow?.UpdateSizeAndPos(NativeWindowRect);

        var rect = NativeWindowRect - GlassFrameThickness;

        _isUpdatingExtend = true;
        try
        {
            Extend = rect.Width + "x" + rect.Height;
        }
        finally
        {
            _isUpdatingExtend = false;
        }

        SetSeparationLayerPos(SWP_NOACTIVATE | SWP_NOZORDER);
    }

    private IntPtr WindowProc(IntPtr windowHandle, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_SIZING:
                if (AspectRatio.HandleSizing(wParam, lParam, GlassFrameThickness))
                {
                    handled = true;
                    return (IntPtr)1;
                }
                break;

            case WM_HOTKEY when wParam.ToInt32() == FitWindowHotkeyId:
                handled = true;
                FitForegroundWindow();
                break;
        }

        return IntPtr.Zero;
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
        ShowInTaskbar = false;
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