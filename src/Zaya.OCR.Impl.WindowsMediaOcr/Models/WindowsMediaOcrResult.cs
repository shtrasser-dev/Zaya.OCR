using Zaya.Primitives;
using Zaya.Primitives.OCR;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Models;

/// <summary>
/// Default implementation of <see cref="IOCRResult"/> for Windows.Media.Ocr results.
/// </summary>
public sealed class WindowsMediaOcrResult : IOCRResult
{
    /// <inheritdoc />
    public IReadOnlyList<IOCRWord> Words { get; }

    /// <inheritdoc />
    public double Confidence { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsMediaOcrResult"/> class.
    /// </summary>
    /// <param name="words">The recognized words.</param>
    /// <param name="confidence">The overall confidence score (0.0 to 1.0).</param>
    public WindowsMediaOcrResult(IReadOnlyList<IOCRWord> words, double confidence)
    {
        Words = words;
        Confidence = confidence;
    }
}
