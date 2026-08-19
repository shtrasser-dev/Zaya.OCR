using Zaya.Primitives;
using Zaya.Primitives.OCR;

namespace Zaya.OCR.Impl.OneOcr.Models;

/// <summary>
/// Default implementation of <see cref="IOCRWord"/> for OneOCR engine results.
/// </summary>
public sealed class OneOcrWord : IOCRWord
{
    /// <summary>
    /// Gets the recognized text of the word.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the oriented bounding box of the word in the image.
    /// </summary>
    public BoundingBox Bounds { get; }

    /// <summary>
    /// Gets the confidence of the word recognition (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrWord"/> class.
    /// </summary>
    /// <param name="text">The recognized text.</param>
    /// <param name="bounds">The oriented bounding box in the image.</param>
    /// <param name="confidence">The confidence score (0.0 to 1.0).</param>
    public OneOcrWord(string text, BoundingBox bounds, double confidence)
    {
        Text = text;
        Bounds = bounds;
        Confidence = confidence;
    }
}
