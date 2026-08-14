using System.Drawing.Imaging;
using System.Text;
using Windows.Media.Ocr;

namespace LookUp.Ocr;

/// <summary>
/// Headless smoke test for the OCR pipeline. Run with <c>LookUp.exe --selftest [outfile]</c>.
/// Renders known text to a bitmap, recognizes it, and reports the round-trip.
/// Because this is a WinExe there's no console, so results are written to a file
/// (and to the console if one happens to be attached).
/// </summary>
internal static class SelfTest
{
    public static async Task<int> Run(string? outputPath)
    {
        var log = new StringBuilder();
        void Line(string s) { log.AppendLine(s); Console.WriteLine(s); }

        Line("LookUp self-test");
        Line("================");

        var languages = OcrEngine.AvailableRecognizerLanguages;
        Line($"Available OCR languages ({languages.Count}):");
        foreach (var lang in languages)
            Line($"  - {lang.DisplayName} [{lang.LanguageTag}]");

        const string expected = "Hello LookUp 12345";
        int exitCode;
        try
        {
            using var bitmap = RenderText(expected);
            var engine = new WindowsOcrEngine();
            Line($"Engine available: {engine.IsAvailable}");

            string recognized = await engine.RecognizeAsync(bitmap);
            Line($"Expected      : {expected}");
            Line($"Recognized(auto): {recognized.Replace(Environment.NewLine, " / ")}");

            // Explicitly forcing the English engine should read Latin text cleanly,
            // which proves the tray "OCR language" override works.
            var enEngine = new WindowsOcrEngine("en-US");
            if (enEngine.IsAvailable)
            {
                string en = await enEngine.RecognizeAsync(bitmap);
                Line($"Recognized(en-US): {en.Replace(Environment.NewLine, " / ")}");
            }

            // The real fix: an un-pinned (Auto) engine should honor the capture-time
            // language hint (normally the keyboard layout) and route Latin text to the
            // Latin recognizer, even on a machine whose default engine is Cyrillic.
            bool enInstalled = OcrEngine.AvailableRecognizerLanguages
                .Any(l => l.LanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase));
            if (enInstalled)
            {
                string hinted = await engine.RecognizeAsync(bitmap, "en-US");
                Line($"Recognized(auto + en-US hint): {hinted.Replace(Environment.NewLine, " / ")}");
                Line(hinted.Contains("Hello", StringComparison.Ordinal)
                    ? "Language routing: OK (hint switched to the Latin recognizer)"
                    : "Language routing: check — hint did not read Latin cleanly");
            }

            bool digitsOk = recognized.Contains("12345");
            bool wordOk = recognized.Contains("LookUp", StringComparison.OrdinalIgnoreCase)
                          || recognized.Contains("Look", StringComparison.OrdinalIgnoreCase);
            exitCode = digitsOk && wordOk ? 0 : 2;
            Line($"Result: {(exitCode == 0 ? "PASS" : "WEAK/FAIL")}");
        }
        catch (Exception ex)
        {
            Line("EXCEPTION: " + ex);
            exitCode = 1;
        }

        outputPath ??= Path.Combine(Path.GetTempPath(), "lookup-selftest.txt");
        try { File.WriteAllText(outputPath, log.ToString()); } catch { /* ignore */ }

        return exitCode;
    }

    private static Bitmap RenderText(string text)
    {
        var bitmap = new Bitmap(640, 160, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.White);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe UI", 34f, FontStyle.Regular);
        g.DrawString(text, font, Brushes.Black, new PointF(16, 48));
        return bitmap;
    }
}
