using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Security.Cryptography;

namespace LookUp.Ocr;

/// <summary>
/// Text recognition via the OCR engine built into Windows 10/11
/// (<c>Windows.Media.Ocr</c>). No external binaries, no language data to ship —
/// it reuses whatever OCR languages the user already has installed.
/// </summary>
internal sealed class WindowsOcrEngine : IOcrEngine
{
    private readonly string? _pinnedLanguageTag;
    private readonly bool _keepLineBreaks;

    /// <summary>
    /// Fallback engine, used when no language is pinned and the capture-time hint
    /// doesn't map to an installed recognizer. Follows the Windows profile.
    /// </summary>
    private readonly OcrEngine? _defaultEngine;

    /// <summary>Engines built on demand per language tag, so switching is instant.</summary>
    private readonly Dictionary<string, OcrEngine?> _byLanguage =
        new(StringComparer.OrdinalIgnoreCase);

    public WindowsOcrEngine(string? pinnedLanguageTag = null, bool keepLineBreaks = true)
    {
        _keepLineBreaks = keepLineBreaks;
        _pinnedLanguageTag = string.IsNullOrWhiteSpace(pinnedLanguageTag)
            ? null
            : pinnedLanguageTag.Trim();
        _defaultEngine = CreateDefaultEngine(_pinnedLanguageTag);
    }

    public bool IsAvailable => _defaultEngine is not null;

    /// <summary>
    /// Builds the fallback engine: an explicitly pinned language if installed,
    /// otherwise the Windows profile languages, otherwise anything available.
    /// </summary>
    private static OcrEngine? CreateDefaultEngine(string? pinnedLanguageTag)
    {
        // 1) Explicit preference from settings, if that language is installed.
        if (!string.IsNullOrWhiteSpace(pinnedLanguageTag))
        {
            try
            {
                var lang = new Language(pinnedLanguageTag);
                if (OcrEngine.IsLanguageSupported(lang))
                {
                    var byPref = OcrEngine.TryCreateFromLanguage(lang);
                    if (byPref is not null)
                        return byPref;
                }
            }
            catch
            {
                // Invalid tag — fall through to auto-detection.
            }
        }

        // 2) Follow the user's Windows display/input languages.
        var byProfile = OcrEngine.TryCreateFromUserProfileLanguages();
        if (byProfile is not null)
            return byProfile;

        // 3) Last resort: first language the machine can recognize at all.
        foreach (var available in OcrEngine.AvailableRecognizerLanguages)
        {
            var engine = OcrEngine.TryCreateFromLanguage(available);
            if (engine is not null)
                return engine;
        }

        return null;
    }

    /// <summary>
    /// Picks which engine handles this capture. A pinned language always wins;
    /// otherwise the capture-time hint (keyboard layout) chooses the script, with
    /// the profile engine as a safety net.
    /// </summary>
    private OcrEngine? ResolveEngine(string? contextLanguageTag)
    {
        if (_pinnedLanguageTag is not null)
            return _defaultEngine; // user chose a fixed language — honor it

        if (string.IsNullOrWhiteSpace(contextLanguageTag))
            return _defaultEngine;

        return GetOrCreateForLanguage(contextLanguageTag!) ?? _defaultEngine;
    }

    private OcrEngine? GetOrCreateForLanguage(string tag)
    {
        if (_byLanguage.TryGetValue(tag, out var cached))
            return cached;

        OcrEngine? engine = BuildForLanguage(tag);
        _byLanguage[tag] = engine; // cache the miss too, so we don't retry every capture
        return engine;
    }

    /// <summary>
    /// Finds an installed recognizer matching <paramref name="tag"/> — first by exact
    /// tag ("ru-RU"), then by primary language ("ru" also matches "ru-RU"; "en"
    /// matches "en-US"/"en-GB"). Returns null if that language isn't installed.
    /// </summary>
    private static OcrEngine? BuildForLanguage(string tag)
    {
        string primary = PrimaryLanguage(tag);

        OcrEngine? FirstMatch(Func<Language, bool> predicate)
        {
            foreach (var lang in OcrEngine.AvailableRecognizerLanguages)
            {
                if (!predicate(lang))
                    continue;
                var engine = OcrEngine.TryCreateFromLanguage(lang);
                if (engine is not null)
                    return engine;
            }
            return null;
        }

        return FirstMatch(l => string.Equals(l.LanguageTag, tag, StringComparison.OrdinalIgnoreCase))
            ?? FirstMatch(l => string.Equals(
                PrimaryLanguage(l.LanguageTag), primary, StringComparison.OrdinalIgnoreCase));
    }

    private static string PrimaryLanguage(string tag)
    {
        int dash = tag.IndexOf('-');
        return dash > 0 ? tag[..dash] : tag;
    }

    public async Task<string> RecognizeAsync(Bitmap image, string? contextLanguageTag = null)
    {
        var engine = ResolveEngine(contextLanguageTag);
        if (engine is null)
            throw new InvalidOperationException(
                "No OCR language is installed. Add one in Windows Settings → " +
                "Time & Language → Language & region → (language) → Options → " +
                "Optional features, then enable Optical character recognition.");

        using var prepared = UpscaleForOcr(image);
        var software = ToSoftwareBitmap(prepared);
        try
        {
            var result = await engine.RecognizeAsync(software);
            return Flatten(result);
        }
        finally
        {
            software.Dispose();
        }
    }

    private string Flatten(OcrResult result)
    {
        var lines = result.Lines.Select(line => string.Join(" ", line.Words.Select(w => w.Text)));
        string separator = _keepLineBreaks ? Environment.NewLine : " ";
        return string.Join(separator, lines).Trim();
    }

    /// <summary>
    /// The Windows OCR engine is tuned for reasonably sized text; tiny captures
    /// (small fonts, low-res regions) recognize far better when scaled up first.
    /// </summary>
    private static Bitmap UpscaleForOcr(Bitmap source)
    {
        const int minShortSide = 320;
        const double maxScale = 4.0;
        const int hardMax = 10_000; // Windows OCR rejects bitmaps larger than this.

        int shortSide = Math.Min(source.Width, source.Height);
        double scale = shortSide < minShortSide
            ? Math.Min(maxScale, (double)minShortSide / Math.Max(1, shortSide))
            : 1.0;

        int width = Math.Min(hardMax, (int)Math.Round(source.Width * scale));
        int height = Math.Min(hardMax, (int)Math.Round(source.Height * scale));

        if (scale <= 1.0 || width <= source.Width)
            return (Bitmap)source.Clone();

        var scaled = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(scaled);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.DrawImage(source, 0, 0, width, height);
        return scaled;
    }

    private static SoftwareBitmap ToSoftwareBitmap(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            int length = data.Stride * bitmap.Height;
            byte[] bytes = new byte[length];
            Marshal.Copy(data.Scan0, bytes, 0, length);

            // 32bppArgb in memory is B,G,R,A (little-endian) — i.e. BGRA8.
            var buffer = CryptographicBuffer.CreateFromByteArray(bytes);
            return SoftwareBitmap.CreateCopyFromBuffer(
                buffer, BitmapPixelFormat.Bgra8, bitmap.Width, bitmap.Height);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }
}
