using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using RegionToShare.Properties;
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

        SetBinding(IsFitWindowHotkeyInUseProperty, new Binding(nameof(MainWindow.IsFitWindowHotkeyInUse)) { Source = mainWindow });

        Settings.Default.PropertyChanged += Settings_PropertyChanged;
        UpdateIsRestartRequired();
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
        nameof(IsFitWindowHotkeyInUse), typeof(bool), typeof(SettingsWindow), new PropertyMetadata(default(bool)));

    public bool IsRestartRequired
    {
        get => (bool)GetValue(IsRestartRequiredProperty);
        set => SetValue(IsRestartRequiredProperty, value);
    }
    public static readonly DependencyProperty IsRestartRequiredProperty = DependencyProperty.Register(
        nameof(IsRestartRequired), typeof(bool), typeof(SettingsWindow), new PropertyMetadata(default(bool)));

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

    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

    private void HotkeyBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Registered hotkeys never reach the window, so they must be released while a new one is captured.
        _mainWindow.AreHotkeysSuspended = true;
    }

    private void HotkeyBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _mainWindow.AreHotkeysSuspended = false;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
