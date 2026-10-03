using System.Globalization;
using RegionToShare.Properties;

namespace RegionToShare;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public App()
    {
        InitializeComponent();

        if (!RegionToShare.MainWindow.ValidateSettings())
        {
            Shutdown();
            return;
        }

        StartupLanguage = Settings.Default.Language;
        ApplyLanguage(StartupLanguage);
    }

    /// <summary>
    /// The language the app was started with; changing it requires a restart.
    /// </summary>
    public static string StartupLanguage { get; private set; } = string.Empty;

    private static void ApplyLanguage(string language)
    {
        if (string.IsNullOrEmpty(language))
            return;

        try
        {
            var culture = CultureInfo.GetCultureInfo(language);

            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            // Invalid setting, stay with the system default.
        }
    }
}
