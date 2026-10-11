using System.Windows;
using System.Windows.Interop;
using ColorDialog = System.Windows.Forms.ColorDialog;
using DialogResult = System.Windows.Forms.DialogResult;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace RegionToShare;

/// <summary>
/// Edits a color in hex format, with a button that opens the Windows color picker.
/// </summary>
public partial class ColorBox
{
    // Keep the custom colors of the color dialog for the session.
    private static int[]? _customColors;

    public static IReadOnlyList<NamedColor> NamedColors { get; } = typeof(System.Windows.Media.Colors)
        .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Where(p => p.PropertyType == typeof(System.Windows.Media.Color))
        .Select(p => new NamedColor(p.Name, (System.Windows.Media.Color)p.GetValue(null)!))
        .OrderBy(c => c.Name, StringComparer.Ordinal)
        .ToList();

    public ColorBox()
    {
        InitializeComponent();
    }

    public IReadOnlyList<NamedColor> Names => NamedColors;

    /// <summary>
    /// The color as hex string, e.g. "#4682B4".
    /// </summary>
    public string? Color
    {
        get => (string?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(string), typeof(ColorBox),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private void PickButton_Click(object sender, RoutedEventArgs e)
    {
        ColorBrushConverter.TryParseColor(Color, out var current);

        using var dialog = new ColorDialog
        {
            AnyColor = true,
            FullOpen = true,
            Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B),
            CustomColors = _customColors ?? Array.Empty<int>()
        };

        var owner = Window.GetWindow(this);
        var result = owner == null ? dialog.ShowDialog() : dialog.ShowDialog(new Win32Window(new WindowInteropHelper(owner).Handle));

        _customColors = dialog.CustomColors;

        if (result != DialogResult.OK)
            return;

        var picked = dialog.Color;
        Color = ColorBrushConverter.ToHex(System.Windows.Media.Color.FromRgb(picked.R, picked.G, picked.B));
    }

    private sealed class Win32Window : IWin32Window
    {
        public Win32Window(IntPtr handle) => Handle = handle;

        public IntPtr Handle { get; }
    }
}

public sealed record NamedColor(string Name, System.Windows.Media.Color Color);

public sealed class ColorValidationRule : System.Windows.Controls.ValidationRule
{
    public override System.Windows.Controls.ValidationResult Validate(object? value, System.Globalization.CultureInfo cultureInfo)
    {
        return ColorBrushConverter.TryParseColor(value as string, out _)
            ? System.Windows.Controls.ValidationResult.ValidResult
            : new System.Windows.Controls.ValidationResult(false, "Invalid color");
    }
}
