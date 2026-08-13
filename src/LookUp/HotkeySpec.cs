namespace LookUp;

/// <summary>
/// A parsed global hotkey: a set of Win32 modifier flags plus a single key.
/// Serialized to/from human strings like "Ctrl+Shift+X".
/// </summary>
internal readonly struct HotkeySpec(uint modifiers, Keys key)
{
    // Win32 modifier flags for RegisterHotKey.
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;

    public uint Modifiers { get; } = modifiers;
    public Keys Key { get; } = key;

    public static HotkeySpec Default => new(ModControl | ModShift, Keys.D);

    public static bool TryParse(string? text, out HotkeySpec spec)
    {
        spec = Default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        uint mods = 0;
        Keys key = Keys.None;

        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control" or "ctl":
                    mods |= ModControl; break;
                case "shift":
                    mods |= ModShift; break;
                case "alt":
                    mods |= ModAlt; break;
                case "win" or "windows" or "super" or "meta":
                    mods |= ModWin; break;
                default:
                    if (!Enum.TryParse<Keys>(raw, ignoreCase: true, out key) || key == Keys.None)
                        return false;
                    break;
            }
        }

        if (key == Keys.None)
            return false;

        spec = new HotkeySpec(mods, key);
        return true;
    }

    public override string ToString()
    {
        var parts = new List<string>(4);
        if ((Modifiers & ModControl) != 0) parts.Add("Ctrl");
        if ((Modifiers & ModAlt) != 0) parts.Add("Alt");
        if ((Modifiers & ModShift) != 0) parts.Add("Shift");
        if ((Modifiers & ModWin) != 0) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}
