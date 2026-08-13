using System.Diagnostics;
using LookUp.Ocr;

namespace LookUp;

/// <summary>
/// Owns the tray icon, the global hotkey, and the capture → OCR → clipboard flow.
/// This is the application's root object; disposing it tears everything down.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private const string GitHubUrl = "https://github.com/DenisDrobyshev/LookUp";

    private readonly AppSettings _settings;
    private readonly NotifyIcon _tray;
    private readonly HotKeyWindow _hotKeyWindow;
    private IOcrEngine _ocr;
    private bool _capturing;

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();
        _ocr = new WindowsOcrEngine(_settings.Language, _settings.KeepLineBreaks);

        _tray = new NotifyIcon
        {
            Icon = AppResources.TrayIcon,
            Text = TrayTooltip(),
            Visible = true,
        };
        _tray.ContextMenuStrip = BuildMenu();
        _tray.DoubleClick += (_, _) => BeginCapture();

        _hotKeyWindow = new HotKeyWindow();
        _hotKeyWindow.HotKeyPressed += (_, _) => BeginCapture();

        if (!_hotKeyWindow.Register(_settings.HotkeySpec))
        {
            ShowBalloon("Hotkey unavailable",
                $"Couldn't register {_settings.HotkeySpec} (another app may own it). " +
                "Double-click the tray icon to capture, or change \"Hotkey\" in the settings file.",
                ToolTipIcon.Warning);
        }

        if (!_ocr.IsAvailable)
        {
            ShowBalloon("No OCR language installed",
                "Enable an OCR language in Windows Settings → Time & Language → Language & region.",
                ToolTipIcon.Warning);
        }
    }

    private string TrayTooltip() =>
        $"LookUp — capture text ({_settings.HotkeySpec})";

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add($"Capture text  ·  {_settings.HotkeySpec}", null, (_, _) => BeginCapture());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(BuildLanguageMenu());

        var autostart = new ToolStripMenuItem("Run at Windows startup")
        {
            Checked = AutoStart.IsEnabled,
            CheckOnClick = true,
        };
        autostart.Click += (_, _) => AutoStart.Set(autostart.Checked);
        menu.Items.Add(autostart);

        menu.Items.Add("Edit settings…", null, (_, _) => _settings.OpenInEditor());
        menu.Items.Add("About / GitHub", null, (_, _) => OpenUrl(GitHubUrl));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit LookUp", null, (_, _) => ExitThread());

        return menu;
    }

    /// <summary>
    /// Windows OCR recognizes one language per pass, so let the user pick which
    /// installed language to use (or "Auto" to follow the Windows display language).
    /// </summary>
    private ToolStripMenuItem BuildLanguageMenu()
    {
        var root = new ToolStripMenuItem("OCR language");

        void AddItem(string label, string tag)
        {
            var item = new ToolStripMenuItem(label)
            {
                Tag = tag,
                Checked = string.Equals(_settings.Language, tag, StringComparison.OrdinalIgnoreCase),
            };
            item.Click += (_, _) => SetLanguage(tag);
            root.DropDownItems.Add(item);
        }

        AddItem("Auto (follow Windows)", "");
        root.DropDownItems.Add(new ToolStripSeparator());
        foreach (var lang in Windows.Media.Ocr.OcrEngine.AvailableRecognizerLanguages)
            AddItem(lang.DisplayName, lang.LanguageTag);

        return root;
    }

    private void SetLanguage(string tag)
    {
        _settings.Language = tag;
        _settings.Save();
        _ocr = new WindowsOcrEngine(_settings.Language, _settings.KeepLineBreaks);
        _tray.ContextMenuStrip = BuildMenu(); // refresh checkmarks
    }

    private async void BeginCapture()
    {
        if (_capturing) return;
        _capturing = true;

        Bitmap? screenshot = null;
        try
        {
            screenshot = ScreenCapture.CaptureVirtualScreen();

            Rectangle? region;
            using (var overlay = new SelectionOverlay(screenshot))
                region = overlay.PickRegion();

            if (region is not { Width: >= 3, Height: >= 3 })
                return; // cancelled or too small to be meaningful

            using var crop = ScreenCapture.Crop(screenshot, region.Value);
            string text = await _ocr.RecognizeAsync(crop);

            if (string.IsNullOrWhiteSpace(text))
            {
                ShowBalloon("No text found", "Couldn't read any text in that area.", ToolTipIcon.Info);
                return;
            }

            Clipboard.SetText(text);
            ShowBalloon("Copied to clipboard", Preview(text), ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            ShowBalloon("LookUp error", ex.Message, ToolTipIcon.Error);
        }
        finally
        {
            screenshot?.Dispose();
            _capturing = false;
        }
    }

    private static string Preview(string text)
    {
        string oneLine = text.ReplaceLineEndings(" ").Trim();
        return oneLine.Length <= 80 ? oneLine : oneLine[..77] + "…";
    }

    private void ShowBalloon(string title, string message, ToolTipIcon icon)
    {
        _tray.BalloonTipTitle = title;
        _tray.BalloonTipText = message;
        _tray.BalloonTipIcon = icon;
        _tray.ShowBalloonTip(2500);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Ignore — no default browser, or blocked.
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotKeyWindow.Dispose();
            if (_tray is not null)
            {
                _tray.Visible = false;
                _tray.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}
