using RegionToShare.Properties;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TomsToolbox.Wpf.Styles;

namespace RegionToShare;

public partial class SettingsWindow
{
    private static SettingsWindow? _instance;

    private readonly MainWindow _mainWindow;

    private SettingsWindow(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;

        InitializeComponent();

        Resources.RegisterDefaultStyles();

        MaxHeight = SystemParameters.WorkArea.Height;

        SetBinding(IsFitWindowHotkeyInUseProperty, new Binding(nameof(MainWindow.IsFitWindowHotkeyInUse)) { Source = mainWindow });

        Settings.Default.PropertyChanged += Settings_PropertyChanged;
        UpdateIsRestartRequired();

        ResolutionsBox.Text = CustomList.ToText(mainWindow.Resolutions);
        AspectRatiosBox.Text = CustomList.ToText(mainWindow.AspectRatios.Where(item => !item.IsFree).Select(item => item.Name));
    }

    public static ICollection<KeyValuePair<string, string>> Languages { get; } = new[]
    {
        new KeyValuePair<string, string>(string.Empty, Properties.Resources.Settings_LanguageSystem),
        new KeyValuePair<string, string>("en", "English"),
        new KeyValuePair<string, string>("de", "Deutsch"),
    };

    public static IValueConverter BooleanToVisibility { get; } = new BooleanToVisibilityConverter();

    public bool IsFitWindowHotkeyInUse
    {
        get => (bool)GetValue(IsFitWindowHotkeyInUseProperty);
        set => SetValue(IsFitWindowHotkeyInUseProperty, value);
    }
    public static readonly DependencyProperty IsFitWindowHotkeyInUseProperty = DependencyProperty.Register(
        nameof(IsFitWindowHotkeyInUse), typeof(bool), typeof(SettingsWindow));

    public bool IsRestartRequired
    {
        get => (bool)GetValue(IsRestartRequiredProperty);
        set => SetValue(IsRestartRequiredProperty, value);
    }
    public static readonly DependencyProperty IsRestartRequiredProperty = DependencyProperty.Register(
        nameof(IsRestartRequired), typeof(bool), typeof(SettingsWindow));

    public static void Open(MainWindow mainWindow)
    {
        if (_instance != null)
        {
            _instance.WindowState = WindowState.Normal;
            _instance.Activate();
            return;
        }

        _instance = new SettingsWindow(mainWindow);
        _instance.Closed += (_, _) => _instance = null;
        _instance.Show();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Settings.Default.PropertyChanged -= Settings_PropertyChanged;
        _mainWindow.AreHotkeysSuspended = false;
        Settings.Default.Save();
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.Language))
        {
            UpdateIsRestartRequired();
        }
    }

    private void UpdateIsRestartRequired()
    {
        IsRestartRequired = Settings.Default.Language != App.StartupLanguage;
    }

    private void HotkeyBox_GotKeyboardFocus(object? sender, KeyboardFocusChangedEventArgs e)
    {
        // Registered hotkeys never reach the window, so they must be released while a new one is captured.
        _mainWindow.AreHotkeysSuspended = true;
    }

    private void HotkeyBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _mainWindow.AreHotkeysSuspended = false;
    }

    private void TabControl_Loaded(object sender, RoutedEventArgs e)
    {
        var tabControl = (TabControl)sender;
        tabControl.Loaded -= TabControl_Loaded;

        // SizeToContent only measures the currently selected tab, so switching tabs would
        // otherwise resize the window. Measure every tab once, fix the window to the
        // tallest one, then disable SizeToContent so the height stays stable.
        var selectedIndex = tabControl.SelectedIndex;
        var maxHeight = 0d;

        for (var i = 0; i < tabControl.Items.Count; i++)
        {
            tabControl.SelectedIndex = i;
            UpdateLayout();

            maxHeight = Math.Max(maxHeight, ActualHeight);
        }

        tabControl.SelectedIndex = selectedIndex;

        SizeToContent = SizeToContent.Manual;
        Height = Math.Min(maxHeight, MaxHeight);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Resolutions_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!Resolution.Save(ResolutionsBox.Text)) 
            return;

        ResolutionsBox.Text = CustomList.ToText(_mainWindow.Resolutions);
    }

    private void AspectRatios_LostFocus(object sender, RoutedEventArgs e)
    {
        if (!AspectRatio.Save(AspectRatiosBox.Text)) 
            return;

        AspectRatiosBox.Text = CustomList.ToText(_mainWindow.AspectRatios.Where(item => !item.IsFree).Select(item => item.Name));
    }
}
