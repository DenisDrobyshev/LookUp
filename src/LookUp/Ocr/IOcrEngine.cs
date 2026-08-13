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

    /// <summary>Recognizes text in <paramref name="image"/> and returns it as plain text.</summary>
    Task<string> RecognizeAsync(Bitmap image);
}
