namespace Zaya.OCR.Models;

/// <summary>
/// Represents the result of an OCR recognition operation.
/// </summary>
public interface IOCRResult
{
    /// <summary>
    /// Gets the list of recognized words with their bounding boxes and confidence.
    /// </summary>
    public IReadOnlyList<IOCRWord> Words { get; }

    /// <summary>
    /// Gets the overall confidence of the recognition (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; }
}
