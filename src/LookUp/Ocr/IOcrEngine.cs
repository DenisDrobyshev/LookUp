namespace LookUp.Ocr;

/// <summary>
/// Abstraction over the text-recognition backend. Today it's the built-in
/// Windows OCR engine; a Tesseract / cloud implementation can slot in behind
/// this same interface when LookUp goes cross-platform.
/// </summary>
internal interface IOcrEngine
{
    /// <summary>True if at least one recognizer language is available.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Recognizes text in <paramref name="image"/> and returns it as plain text.
    /// </summary>
    /// <param name="contextLanguageTag">
    /// Optional BCP-47 hint (e.g. "en-US", "ru-RU") for which script to prefer when
    /// glyphs are ambiguous between alphabets. Typically the keyboard layout active
    /// when the capture was taken. Ignored if the user has pinned a fixed language.
    /// </param>
    Task<string> RecognizeAsync(Bitmap image, string? contextLanguageTag = null);
}
