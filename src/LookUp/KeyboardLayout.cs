using System.Globalization;
using System.Runtime.InteropServices;

namespace LookUp;

/// <summary>
/// Reads the keyboard layout (input language) active in the foreground application.
/// LookUp uses this to decide whether an ambiguous capture — text made of glyphs
/// that exist in both alphabets, like "CAT" vs. "САТ" — should be read as Latin or
/// Cyrillic. Whatever script you're currently typing in is almost always the script
/// you're looking at, so it's a good hint for the one-language-per-pass OCR engine.
/// </summary>
internal static class KeyboardLayout
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    /// <summary>
    /// BCP-47 language tag of the foreground window's active input layout
    /// (e.g. "en-US", "ru-RU"), or <c>null</c> if it can't be determined. Must be
    /// called before LookUp shows its own overlay, while the target app is still
    /// in the foreground.
    /// </summary>
    public static string? ForegroundInputLanguageTag()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            uint threadId = hwnd == IntPtr.Zero ? 0 : GetWindowThreadProcessId(hwnd, out _);
            IntPtr hkl = GetKeyboardLayout(threadId);

            // The low word of an HKL is the input-language identifier (LANGID),
            // which doubles as an LCID: 0x0409 → en-US, 0x0419 → ru-RU, etc.
            int langId = (int)((long)hkl & 0xFFFF);
            if (langId == 0)
                return null;

            return CultureInfo.GetCultureInfo(langId).Name;
        }
        catch
        {
            // Unknown/custom layout, or an LCID with no matching culture —
            // fall back to the default engine.
            return null;
        }
    }
}
