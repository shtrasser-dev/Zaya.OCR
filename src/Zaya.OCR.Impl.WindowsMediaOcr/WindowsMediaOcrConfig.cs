using Zaya.OCR.Impl.WindowsMediaOcr.Constants;

namespace Zaya.OCR.Impl.WindowsMediaOcr;

/// <summary>
/// Typed configuration for <see cref="Services.WindowsMediaOcrService"/>.
/// Converts to the dictionary format expected by <c>CreateSessionAsync</c>.
/// </summary>
public sealed class WindowsMediaOcrConfig
{
    /// <summary>
    /// Gets or sets the OCR language as a BCP-47 tag, or <c>auto</c> to use
    /// the user profile languages via <c>OcrEngine.TryCreateFromUserProfileLanguages</c>.
    /// Default is <c>auto</c>.
    /// </summary>
    public string Language { get; set; } = SettingsConstants.Auto;

    /// <summary>
    /// Converts the typed configuration to the dictionary format accepted by
    /// <see cref="Services.WindowsMediaOcrService.CreateSessionAsync(System.Collections.Generic.IReadOnlyDictionary{string, object}, System.Threading.CancellationToken)"/>.
    /// </summary>
    public Dictionary<string, object?> ToDictionary() => new()
    {
        [SettingsConstants.Language] = Language,
    };
}
