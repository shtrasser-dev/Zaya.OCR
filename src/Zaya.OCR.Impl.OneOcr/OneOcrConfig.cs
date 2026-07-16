namespace Zaya.OCR.Impl.OneOcr;

/// <summary>
/// Specifies how the OneOCR engine files are obtained.
/// </summary>
public enum OneOcrSource
{
    /// <summary>
    /// Auto-detect from the Windows 11 SnippingTool installation.
    /// </summary>
    SnippingTool,

    /// <summary>
    /// Load from a local directory containing oneocr.dll, onnxruntime.dll, and oneocr.onemodel.
    /// </summary>
    Directory,

    /// <summary>
    /// Download from a URL (not yet implemented).
    /// </summary>
    Url
}

/// <summary>
/// Typed configuration for <see cref="Services.OneOcrService"/>.
/// Converts to the dictionary format expected by <c>InitializeAsync</c>.
/// </summary>
public class OneOcrConfig
{
    /// <summary>
    /// Gets or sets the engine source. Default is <see cref="OneOcrSource.SnippingTool"/>.
    /// </summary>
    public OneOcrSource Source { get; set; } = OneOcrSource.SnippingTool;

    /// <summary>
    /// Gets or sets the local directory path containing the engine files. Required when <see cref="Source"/> is <see cref="OneOcrSource.Directory"/>.
    /// </summary>
    public string? DirectoryPath { get; set; }

    /// <summary>
    /// Gets or sets the URL to download the engine from. Required when <see cref="Source"/> is <see cref="OneOcrSource.Url"/>.
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Gets or sets the cache directory for extracted engine files. Default is <c>%TEMP%\Zaya\OneOcr</c>.
    /// </summary>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Converts the typed configuration to the dictionary format accepted by <c>InitializeAsync</c>.
    /// </summary>
    /// <returns>A dictionary with string keys and object values.</returns>
    public Dictionary<string, object?> ToDictionary() => new()
    {
        ["source"] = Source switch
        {
            OneOcrSource.SnippingTool => "snippingtool",
            OneOcrSource.Directory => "directory",
            OneOcrSource.Url => "url",
            _ => "snippingtool"
        },
        ["directoryPath"] = DirectoryPath,
        ["downloadUrl"] = DownloadUrl,
        ["cacheDirectory"] = CacheDirectory,
    };
}
