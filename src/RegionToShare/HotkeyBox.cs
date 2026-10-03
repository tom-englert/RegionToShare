using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RegionToShare;

/// <summary>
/// A text box that captures a key combination instead of text.
/// </summary>
public class HotkeyBox : TextBox
{
    public HotkeyBox()
    {
        IsReadOnly = true;
        IsReadOnlyCaretVisible = false;
        IsUndoEnabled = false;
    }

    /// <summary>
    /// The hotkey in the culture invariant settings format.
    /// </summary>
    public string? Hotkey
    {
        get => (string?)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }
    public static readonly DependencyProperty HotkeyProperty = DependencyProperty.Register(nameof(Hotkey), typeof(string), typeof(HotkeyBox),
        new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (d, _) => ((HotkeyBox)d).UpdateText()));

    public string? NoneText
    {
        get => (string?)GetValue(NoneTextProperty);
        set => SetValue(NoneTextProperty, value);
    }
    public static readonly DependencyProperty NoneTextProperty = DependencyProperty.Register(nameof(NoneText), typeof(string), typeof(HotkeyBox),
        new FrameworkPropertyMetadata(default(string), (d, _) => ((HotkeyBox)d).UpdateText()));

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        var key = e.Key switch
        {
            Key.System => e.SystemKey,
            Key.ImeProcessed => e.ImeProcessedKey,
            _ => e.Key
        };

        var modifiers = Keyboard.Modifiers;

        // Keyboard.Modifiers does not report the Windows key.
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
        {
            modifiers |= ModifierKeys.Windows;
        }

        if (modifiers == ModifierKeys.None && key is Key.Tab or Key.Escape)
            return;

        e.Handled = true;

        switch (key)
        {
            case Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin:
                return;

            case Key.Delete or Key.Back when modifiers == ModifierKeys.None:
                Hotkey = string.Empty;
                return;
        }

        // Plain keys would block typing in all other applications, function keys are fine.
        if (modifiers == ModifierKeys.None && key is not (>= Key.F1 and <= Key.F24))
            return;

        Hotkey = new Hotkey(modifiers, key).ToString();
    }

    private void UpdateText()
    {
        var hotkey = RegionToShare.Hotkey.Parse(Hotkey);

        Text = hotkey.IsNone ? NoneText : hotkey.ToDisplayString();
    }
}
