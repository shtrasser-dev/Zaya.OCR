using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Models;

/// <summary>
/// Default implementation of <see cref="IOCRWord"/> for Windows.Media.Ocr results.
/// </summary>
public sealed class WindowsMediaOcrWord : IOCRWord
{
    /// <inheritdoc />
    public string Text { get; }

    /// <inheritdoc />
    public BoundingBox Bounds { get; }

    /// <inheritdoc />
    public double Confidence { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsMediaOcrWord"/> class.
    /// </summary>
    /// <param name="text">The recognized text.</param>
    /// <param name="bounds">The oriented bounding box in the image.</param>
    /// <param name="confidence">The confidence score (0.0 to 1.0).</param>
    public WindowsMediaOcrWord(string text, BoundingBox bounds, double confidence)
    {
        Text = text;
        Bounds = bounds;
        Confidence = confidence;
    }
}
