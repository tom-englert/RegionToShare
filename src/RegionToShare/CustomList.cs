using System.Globalization;
using TomsToolbox.Essentials;
using static RegionToShare.NativeMethods;

namespace RegionToShare;

/// <summary>
/// Helpers for user editable lists that are stored as a single string in the settings.
/// </summary>
public static class CustomList
{
    public static string[] Parse(string? text, Func<string, string?> normalize)
    {
        return (text ?? string.Empty)
            .Split([';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => normalize(item.Trim()))
            .ExceptNullItems()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    // An empty value means "use the defaults", so future updates can change the default lists.
    public static string Serialize(string[] items, string[] defaultItems)
    {
        return items.Length == 0 || items.Order().SequenceEqual(defaultItems.Order()) ? string.Empty : string.Join(";", items);
    }

    public static string ToText(IEnumerable<string> items) => string.Join(Environment.NewLine, items);

    public static string[] Load(string? settingValue, string[] defaultItems, Func<string, string?> normalize)
    {
        var items = Parse(settingValue, normalize);

        return items.Length > 0 ? items : defaultItems;
    }

    public static void Replace<T>(ICollection<T> target, IEnumerable<T> items)
    {
        var newItems = items.ToArray();

        target.Clear();

        foreach (var item in newItems)
        {
            target.Add(item);
        }
    }

    public static bool TryParse(string? value, char separator, out SIZE size)
    {
        size = default;

        var parts = value?.Split(separator);

        if (parts is not { Length: 2 }
            || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
            || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height)
            || width <= 0 || height <= 0)
            return false;

        size = new SIZE(width, height);

        return true;
    }
}