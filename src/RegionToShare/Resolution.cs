using RegionToShare.Properties;
using static RegionToShare.NativeMethods;

namespace RegionToShare;

/// <summary>
/// Resolutions in the format "1920x1080", and the user editable list of them.
/// </summary>
public static class Resolution
{
    private static readonly string[] Defaults = ["1024x768", "1280x1024", "1600x1200", "1920x1080", "2560x1440"];

    public static string[] Load()
    {
        return CustomList.Load(Settings.Default.Resolutions, Defaults, Normalize);
    }

    public static bool Save(string? text)
    {
        var newValue = CustomList.Serialize(CustomList.Parse(text, Normalize), Defaults);

        if (string.Equals(newValue, Settings.Default.Resolutions, StringComparison.OrdinalIgnoreCase))
            return false;

        Settings.Default.Resolutions = newValue;

        return true;
    }

    public static bool TryParse(string? value, out SIZE size)
    {
        return CustomList.TryParse(value, 'x', out size);
    }

    private static string? Normalize(string item) => TryParse(item, out _) ? item : null;
}