using System.Windows;
using RegionToShare.Properties;

namespace RegionToShare;

public sealed class AspectRatio
{
    public static readonly AspectRatio Free = new(string.Empty, 0);

    private AspectRatio(string name, double value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    /// <summary>
    /// Width divided by height, or 0 for <see cref="Free"/>.
    /// </summary>
    public double Value { get; }

    public bool IsFree => Value <= 0;

    private static readonly string[] Defaults = ["4:3", "5:4", "16:9", "16:10"];

    /// <summary>
    /// Loads the user editable list, always starting with <see cref="Free"/>.
    /// </summary>
    public static IEnumerable<AspectRatio> Load()
    {
        var items = CustomList.Load(Settings.Default.AspectRatios, Defaults, Normalize);

        return new[] { Free }.Concat(items.Select(item => TryParse(item, out var ratio) ? ratio : Free));
    }

    public static void Save(string? text)
    {
        var newValue = CustomList.Serialize(CustomList.Parse(text, Normalize), Defaults);

        if (string.Equals(newValue, Settings.Default.AspectRatios, StringComparison.OrdinalIgnoreCase))
            return;

        Settings.Default.AspectRatios = newValue;
    }

    private static string? Normalize(string item) => TryParse(item, out var ratio) && !ratio.IsFree ? ratio.Name : null;

    public static bool TryParse(string? text, out AspectRatio ratio)
    {
        ratio = Free;

        if (!CustomList.TryParse(text, ':', out var size))
            return false;

        var value = (double)size.Width / size.Height;

        if (double.IsInfinity(value) || double.IsNaN(value))
            return false;

        ratio = new AspectRatio($"{size.Width}:{size.Height}", value);

        return true;
    }

    /// <summary>
    /// Adjusts a window rectangle during a sizing operation, so the region inside the window keeps this aspect ratio.
    /// </summary>
    /// <param name="rect">The window rectangle proposed by WM_SIZING.</param>
    /// <param name="edge">The edge being dragged (WMSZ_*).</param>
    /// <param name="inset">The distance from the window rectangle to the region.</param>
    public void AdjustSizingRect(ref NativeMethods.RECT rect, int edge, Thickness inset)
    {
        if (IsFree)
            return;

        var horizontalInset = (int)(inset.Left + inset.Right);
        var verticalInset = (int)(inset.Top + inset.Bottom);

        var width = rect.Width - horizontalInset;
        var height = rect.Height - verticalInset;

        switch (edge)
        {
            case NativeMethods.WMSZ_TOP:
            case NativeMethods.WMSZ_BOTTOM:
                rect.Right = rect.Left + (int)Math.Round(height * Value) + horizontalInset;
                break;

            case NativeMethods.WMSZ_TOPLEFT:
            case NativeMethods.WMSZ_TOPRIGHT:
                rect.Top = rect.Bottom - (int)Math.Round(width / Value) - verticalInset;
                break;

            default:
                rect.Bottom = rect.Top + (int)Math.Round(width / Value) + verticalInset;
                break;
        }
    }

    public int HeightFromWidth(int width) => IsFree ? 0 : (int)Math.Round(width / Value);

    public override string ToString() => IsFree ? Resources.AspectRatio_Free : Name;
}
