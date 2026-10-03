using System.Globalization;
using System.Windows;

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

    public static bool TryParse(string? text, out AspectRatio ratio)
    {
        ratio = Free;

        var parts = text?.Split(':');

        if (parts is not { Length: 2 }
            || !double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var width)
            || !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var height)
            || width <= 0 || height <= 0)
            return false;

        ratio = new AspectRatio(parts[0].Trim() + ":" + parts[1].Trim(), width / height);
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

    public override string ToString() => IsFree ? Properties.Resources.AspectRatio_Free : Name;
}
