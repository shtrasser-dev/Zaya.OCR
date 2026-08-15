using Zaya.OCR.Models;
using Zaya.Primitives;

namespace Zaya.OCR.Services;

/// <summary>
/// Merges individually recognized words from an <see cref="IOCRResult"/>
/// into structured text blocks (paragraphs and lines) using configurable layout rules.
/// </summary>
public interface ITextLayoutService : IDisposable
{
    /// <summary>
    /// Gets a unique identifier for this layout engine (e.g., "simple", "advanced").
    /// Used for profile serialization and engine lookup.
    /// </summary>
    string EngineId { get; }

    /// <summary>
    /// Gets the UI display name for this layout engine (localized).
    /// </summary>
    LocalizedString DisplayName { get; }

    /// <summary>
    /// Gets the UI description for this layout engine (localized).
    /// </summary>
    LocalizedString Description { get; }

    /// <summary>
    /// Gets whether this text layout engine is available on the current system.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets the list of engine-specific settings that can be configured via UI.
    /// </summary>
    IReadOnlyList<SettingDescriptor> Settings { get; }

    /// <summary>
    /// Creates a new text layout session with the default engine settings.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel session creation.</param>
    /// <returns>An active text layout session ready to process text.</returns>
    Task<ITextLayoutSession> CreateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new text layout session with the specified engine settings.
    /// </summary>
    /// <param name="engineSettings">Engine-specific settings dictionary, or <c>null</c> for defaults.</param>
    /// <param name="cancellationToken">Token to cancel session creation.</param>
    /// <returns>An active text layout session ready to process text.</returns>
    Task<ITextLayoutSession> CreateSessionAsync(IReadOnlyDictionary<string, object> engineSettings, CancellationToken cancellationToken = default);
}