using System.Windows;
using System.Windows.Input;
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

    public ColorBox()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The color as hex string, e.g. "#4682B4".
    /// </summary>
    public string? Color
    {
        get => (string?)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(string), typeof(ColorBox),
        new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private void ColorTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Show color names like "Red" as hex, too.
        if (ColorBrushConverter.TryParseColor(Color, out var color))
        {
            Color = ColorBrushConverter.ToHex(color);
        }
    }

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
