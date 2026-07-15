using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.OneOcr.Models;

/// <summary>
/// Default implementation of <see cref="IOCRResult"/> for OneOCR engine results.
/// </summary>
public sealed class OneOcrResult : IOCRResult
{
    /// <summary>
    /// Gets the list of recognized words with their bounding boxes and confidence.
    /// </summary>
    public IReadOnlyList<IOCRWord> Words { get; }

    /// <summary>
    /// Gets the overall confidence of the recognition (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrResult"/> class.
    /// </summary>
    /// <param name="words">The recognized words.</param>
    /// <param name="confidence">The overall confidence score (0.0 to 1.0).</param>
    public OneOcrResult(IReadOnlyList<IOCRWord> words, double confidence)
    {
        Words = words;
        Confidence = confidence;
    }
}
