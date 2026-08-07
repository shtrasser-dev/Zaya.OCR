namespace Zaya.OCR.Models;

/// <summary>
/// Represents a single recognized word with its text, bounding box, and confidence score.
/// </summary>
public interface IOCRWord
{
    /// <summary>
    /// Gets the recognized text of the word.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets the oriented bounding box of the word in the image.
    /// </summary>
    BoundingBox Bounds { get; }

    /// <summary>
    /// Gets the confidence of the word recognition (0.0 to 1.0).
    /// </summary>
    double Confidence { get; }
}
