using Zaya.Primitives;
using Zaya.Primitives.Settings;

namespace Zaya.OCR.Services;

/// <summary>
/// Provides text recognition (OCR) capabilities.
/// Create sessions with <see cref="CreateSessionAsync(IReadOnlyDictionary{string, object}, CancellationToken)"/> to perform recognition
/// with a fixed set of options.
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
    /// Gets whether this OCR engine is available on the current system.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the list of engine-specific settings that can be configured via UI.
    /// </summary>
    IReadOnlyList<SettingDescriptor> Settings { get; }

    /// <summary>
    /// Gets the pixel format preferred by this OCR engine.
    /// The caller (e.g., a screenshot module) can use this to deliver pixels
    /// in the optimal format without extra conversion.
    /// </summary>
    PixelFormat PreferredPixelFormat { get; }

    /// <summary>
    /// Creates a new OCR session with the default engine settings.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel session creation.</param>
    /// <returns>An active OCR session ready to recognize text.</returns>
    Task<IOCRSession> CreateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new OCR session with the specified engine settings.
    /// </summary>
    /// <param name="engineSettings">Engine-specific settings dictionary, or <c>null</c> for defaults.</param>
    /// <param name="cancellationToken">Token to cancel session creation.</param>
    /// <returns>An active OCR session ready to recognize text.</returns>
    Task<IOCRSession> CreateSessionAsync(IReadOnlyDictionary<string, object> engineSettings, CancellationToken cancellationToken = default);
}