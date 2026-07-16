using Zaya.Primitives;

namespace Zaya.OCR.Services;

/// <summary>
/// Provides text recognition (OCR) capabilities.
/// Create sessions with <see cref="CreateSessionAsync"/> to perform recognition
/// with a fixed set of options.
/// Call <see cref="InitializeAsync"/> with engine-specific settings before use.
/// </summary>
public interface IOCRService : IDisposable
{
    /// <summary>
    /// Gets a unique identifier for this OCR engine (e.g., "oneocr", "tesseract").
    /// Used for profile serialization and engine lookup.
    /// </summary>
    string EngineId { get; }

    /// <summary>
    /// Gets the UI display name for this engine (localized).
    /// </summary>
    LocalizedString DisplayName { get; }

    /// <summary>
    /// Gets the UI description for this engine (localized).
    /// </summary>
    LocalizedString Description { get; }

    /// <summary>
    /// Gets the list of engine-specific settings that can be configured via UI.
    /// </summary>
    IReadOnlyList<SettingDescriptor> Settings { get; }

    /// <summary>
    /// Initializes the engine with the specified settings.
    /// Must be called before <see cref="CreateSessionAsync"/>.
    /// Throws <see cref="LocalizedException"/> on failure.
    /// </summary>
    Task InitializeAsync(IReadOnlyDictionary<string, object?>? engineSettings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the pixel format preferred by this OCR engine.
    /// The caller (e.g., a screenshot module) can use this to deliver pixels
    /// in the optimal format without extra conversion.
    /// </summary>
    PixelFormat PreferredPixelFormat { get; }

    /// <summary>
    /// Gets whether this OCR engine is initialized and ready.
    /// Returns <c>true</c> only after successful <see cref="InitializeAsync"/>.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Creates a new OCR session.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel session creation.</param>
    /// <returns>An active OCR session ready to recognize text.</returns>
    Task<IOCRSession> CreateSessionAsync(CancellationToken cancellationToken = default);
}
