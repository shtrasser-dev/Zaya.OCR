using Zaya.Primitives;

namespace Zaya.OCR.Models;

/// <summary>
/// Configuration options for OCR recognition operations.
/// Options that are not supported by a particular engine implementation are silently ignored.
/// When <see cref="Width"/>, <see cref="Height"/>, <see cref="Stride"/>, and <see cref="PixelFormat"/>
/// are set, the <c>byte[]</c> passed to <see cref="Services.IOCRSession.RecognizeAsync"/>
/// is treated as raw pixel data rather than an encoded image (PNG, JPEG, BMP).
/// </summary>
public class OcrOptions
{
    /// <summary>
    /// Gets or sets the language tag in BCP-47 format (e.g., "en", "ru-RU", "de-DE").
    /// When <c>null</c>, the engine uses automatic language detection.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets whether word-level confidence scores are returned.
    /// When <c>false</c>, only the overall result confidence is provided.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnableWordLevelConfidence { get; set; } = true;

    /// <summary>
    /// Gets or sets the width of the raw pixel data in pixels.
    /// When <c>null</c>, the <c>byte[]</c> is treated as an encoded image.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the raw pixel data in pixels.
    /// When <c>null</c>, the <c>byte[]</c> is treated as an encoded image.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the stride (row pitch) of the raw pixel data in bytes.
    /// When <c>null</c>, <c>Width * PixelFormat.BytesPerPixel</c> is used.
    /// </summary>
    public int? Stride { get; set; }

    /// <summary>
    /// Gets or sets the pixel format of the raw pixel data.
    /// Required when <see cref="Width"/> and <see cref="Height"/> are set.
    /// </summary>
    public PixelFormat? PixelFormat { get; set; }
}
