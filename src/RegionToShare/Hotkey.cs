using System.Windows.Input;
using RegionToShare.Properties;
using static RegionToShare.NativeMethods;

namespace RegionToShare;

public readonly struct Hotkey
{
    public static readonly Hotkey None = default;

    public Hotkey(ModifierKeys modifiers, Key key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    public ModifierKeys Modifiers { get; }

    public Key Key { get; }

    public bool IsNone => Key == Key.None;

    public uint NativeModifiers =>
        (Modifiers.HasFlag(ModifierKeys.Alt) ? MOD_ALT : 0)
        | (Modifiers.HasFlag(ModifierKeys.Control) ? MOD_CONTROL : 0)
        | (Modifiers.HasFlag(ModifierKeys.Shift) ? MOD_SHIFT : 0)
        | (Modifiers.HasFlag(ModifierKeys.Windows) ? MOD_WIN : 0);

    public uint NativeKey => (uint)KeyInterop.VirtualKeyFromKey(Key);

    /// <summary>
    /// Parses the culture invariant format used in the settings, e.g. "Ctrl+Win+W".
    /// </summary>
    public static Hotkey Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return None;

        var modifiers = ModifierKeys.None;
        var key = Key.None;

        foreach (var part in value!.Split('+').Select(item => item.Trim()))
        {
            switch (part)
            {
                case "Ctrl":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "Alt":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "Shift":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "Win":
                    modifiers |= ModifierKeys.Windows;
                    break;
                default:
                    if (!Enum.TryParse(part, out key))
                        return None;
                    break;
            }
        }

        return new Hotkey(modifiers, key);
    }

    public override string ToString() => Format("Ctrl", "Alt", "Shift", "Win", Key.ToString());

    public string ToDisplayString() => Format(Resources.Key_Ctrl, Resources.Key_Alt, Resources.Key_Shift, Resources.Key_Win, KeyDisplayName);

    private string KeyDisplayName => Key switch
    {
        >= Key.D0 and <= Key.D9 => ((char)('0' + (Key - Key.D0))).ToString(),
        >= Key.NumPad0 and <= Key.NumPad9 => "Num " + (Key - Key.NumPad0),
        _ => Key.ToString()
    };

    private string Format(string ctrl, string alt, string shift, string win, string key)
    {
        if (IsNone)
            return string.Empty;

        var parts = new List<string>();

        if (Modifiers.HasFlag(ModifierKeys.Control))
            parts.Add(ctrl);
        if (Modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add(alt);
        if (Modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add(shift);
        if (Modifiers.HasFlag(ModifierKeys.Windows))
            parts.Add(win);

        parts.Add(key);

        return string.Join("+", parts);
    }
}
