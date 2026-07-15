using Zaya.OCR.Models;
using Zaya.Primitives;

namespace Zaya.OCR.Services;

/// <summary>
/// Provides text recognition (OCR) capabilities.
/// Create sessions with <see cref="CreateSessionAsync"/> to perform recognition
/// with a fixed set of options.
/// </summary>
public interface IOCRService
{
    /// <summary>
    /// Gets a unique identifier for this OCR engine (e.g., "oneocr", "tesseract").
    /// Used for profile serialization and engine lookup.
    /// </summary>
    string EngineId { get; }

    /// <summary>
    /// Gets the pixel format preferred by this OCR engine.
    /// The caller (e.g., a screenshot module) can use this to deliver pixels
    /// in the optimal format without extra conversion.
    /// </summary>
    PixelFormat PreferredPixelFormat { get; }

    /// <summary>
    /// Gets whether this OCR engine is available on the current system.
    /// A lightweight check that does not initialize the engine or download models.
    /// Returns <c>false</c> when the required platform APIs, runtimes,
    /// or hardware capabilities are not present.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Creates a new OCR session with the specified options.
    /// The session holds the options and can be used for multiple recognition calls.
    /// </summary>
    /// <param name="options">Recognition options, or <c>null</c> for defaults.</param>
    /// <param name="cancellationToken">Token to cancel session creation.</param>
    /// <returns>An active OCR session ready to recognize text.</returns>
    Task<IOCRSession> CreateSessionAsync(OcrOptions? options = null, CancellationToken cancellationToken = default);
}
