using Zaya.OCR.Impl.OneOcr.Models;

namespace Zaya.OCR.Impl.OneOcr.Services;

/// <summary>
/// Native OneOCR engine loaded from Snipping Tool, a local directory, or a download URL.
/// </summary>
internal interface IOneOcrEngine : IDisposable
{
    /// <summary>
    /// Gets whether the engine was loaded successfully and can run recognition.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the path to the loaded <c>oneocr.onemodel</c> file, when known.
    /// </summary>
    string? ModelPath { get; }

    /// <summary>
    /// Recognizes text in a BGRA32 pixel buffer.
    /// </summary>
    /// <param name="bgraPixels">Image pixels in BGRA32 layout.</param>
    /// <param name="width">Image width in pixels.</param>
    /// <param name="height">Image height in pixels.</param>
    /// <param name="stride">Row stride in bytes.</param>
    /// <param name="minConfidence">Minimum word confidence in the range 0–1; lower-confidence words are dropped.</param>
    /// <returns>Recognized words that meet <paramref name="minConfidence"/>.</returns>
    NativeWord[] Recognize(byte[] bgraPixels, int width, int height, int stride, double minConfidence = 0);
}
