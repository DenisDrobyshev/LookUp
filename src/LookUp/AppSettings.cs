using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LookUp;

/// <summary>
/// User-editable settings, stored as JSON in %APPDATA%\LookUp\settings.json.
/// Kept intentionally tiny so the file is easy to hand-edit.
/// </summary>
internal sealed class AppSettings
{
    /// <summary>Global capture hotkey, e.g. "Ctrl+Shift+X".</summary>
    public string Hotkey { get; set; } = HotkeySpec.Default.ToString();

    /// <summary>
    /// BCP-47 language tag to pin OCR to a fixed language (e.g. "en", "ru"). Empty =
    /// Auto: follow the keyboard layout active at capture time, falling back to the
    /// Windows display language when that layout has no OCR recognizer installed.
    /// </summary>
    public string Language { get; set; } = "";

    /// <summary>Insert a blank line between recognized text blocks. Off = plain line breaks.</summary>
    public bool KeepLineBreaks { get; set; } = true;

    [JsonIgnore]
    public HotkeySpec HotkeySpec =>
        HotkeySpec.TryParse(Hotkey, out var spec) ? spec : LookUp.HotkeySpec.Default;

    // ---- persistence ----

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public static string Directory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LookUp");

    public static string FilePath => Path.Combine(Directory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null)
                    return loaded;
            }
        }
        catch
        {
            // Corrupt or unreadable settings should never stop the app from starting.
        }

        var fresh = new AppSettings();
        fresh.Save();
        return fresh;
    }

    public void Save()
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // Best effort; not fatal.
        }
    }

    public void OpenInEditor()
    {
        Save();
        try
        {
            Process.Start(new ProcessStartInfo(FilePath) { UseShellExecute = true });
        }
        catch
        {
            // No default editor associated; ignore.
        }
    }
}
