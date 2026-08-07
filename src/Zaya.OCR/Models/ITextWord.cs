namespace Zaya.OCR.Models;

/// <summary>
/// A layout-facing word with text, bounds, and confidence (typically from OCR).
/// </summary>
public interface ITextWord
{
    /// <summary>Recognized / display text.</summary>
    string Text { get; }

    /// <summary>Oriented bounding box in image space.</summary>
    BoundingBox Bounds { get; }

    /// <summary>Recognition confidence in [0, 1].</summary>
    double Confidence { get; }
}
