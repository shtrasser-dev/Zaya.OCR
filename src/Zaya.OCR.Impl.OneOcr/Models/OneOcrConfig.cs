using Zaya.OCR.Impl.OneOcr;
using Zaya.OCR.Impl.OneOcr.Constants;

namespace Zaya.OCR.Impl.OneOcr.Models;

/// <summary>
/// Typed configuration for <see cref="OneOcrService"/>.
/// Converts to the dictionary format expected by <c>CreateSessionAsync</c>.
/// </summary>
/// <remarks>
/// Maps to the same keys as <see cref="OneOcrService.Settings"/>:
/// <c>source</c>, <c>directoryPath</c>, <c>downloadUrl</c>, <c>cacheDirectory</c>, <c>minConfidence</c>.
/// </remarks>
public class OneOcrConfig
{
    /// <summary>
    /// Gets or sets the engine source. Default is <see cref="OneOcrSource.Auto"/>.
    /// </summary>
    public OneOcrSource Source { get; set; } = OneOcrSource.Auto;

    /// <summary>
    /// Gets or sets the local directory path containing the engine files. Required when <see cref="Source"/> is <see cref="OneOcrSource.Directory"/>.
    /// </summary>
    public string? DirectoryPath { get; set; }

    /// <summary>
    /// Gets or sets the URL to download the engine from. Required when <see cref="Source"/> is <see cref="OneOcrSource.Url"/>;
    /// also used as fallback when <see cref="Source"/> is <see cref="OneOcrSource.Auto"/>.
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Gets or sets the cache directory for extracted engine files. Default is <c>%TEMP%\Zaya\OneOcr</c>.
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets the minimum word confidence as a percentage (0–100).
    /// Default is <c>70</c> to match <see cref="OneOcrService.Settings"/>.
    /// </summary>
    public int MinConfidence { get; set; } = 70;

    /// <summary>
    /// Converts the typed configuration to the dictionary format accepted by
    /// <see cref="OneOcrService.CreateSessionAsync(IReadOnlyDictionary{string, object}, CancellationToken)"/>.
    /// </summary>
    /// <returns>A dictionary with string keys and object values.</returns>
    public Dictionary<string, object?> ToDictionary() => new()
    {
        [SettingsConstants.Source] = Source switch
        {
            OneOcrSource.Auto => SettingsConstants.Auto,
            OneOcrSource.SnippingTool => SettingsConstants.SnippingTool,
            OneOcrSource.Directory => SettingsConstants.Directory,
            OneOcrSource.Url => SettingsConstants.Url,
            _ => SettingsConstants.Auto
        },
        [SettingsConstants.DirectoryPath] = DirectoryPath,
        [SettingsConstants.DownloadUrl] = DownloadUrl,
        [SettingsConstants.CacheDirectory] = CacheDirectory,
        [SettingsConstants.MinConfidence] = MinConfidence,
    };
}
