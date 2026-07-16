using Zaya.OCR.Impl.OneOcr.Models;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Services;

/// <summary>
/// OneOCR engine implementation of <see cref="IOCRService"/> using P/Invoke
/// into the native <c>oneocr.dll</c> (Windows 11 SnippingTool OCR engine).
/// No WinRT, no Windows App SDK, no package identity required.
/// Call <see cref="InitializeAsync"/> before creating sessions.
/// </summary>
public sealed class OneOcrService : IOCRService
{
    private OneOcrEngine? _engine;
    private bool _disposed;

    private static LocalizedString Loc(string key)
        => new(key, culture => Properties.Resources.ResourceManager.GetString(key, culture)!);

    /// <inheritdoc />
    public string EngineId => "oneocr";

    /// <inheritdoc />
    public LocalizedString DisplayName => Loc("Ocr_EngineName");

    /// <inheritdoc />
    public LocalizedString Description => Loc("Ocr_EngineDesc");

    /// <inheritdoc />
    public PixelFormat PreferredPixelFormat => PixelFormat.Bgra32;

    /// <inheritdoc />
    public IReadOnlyList<SettingDescriptor> Settings { get; } = [
        new EnumSettingDescriptor("source", Loc("Ocr_Source"))
        {
            Description = Loc("Ocr_Source_Desc"),
            IsRequired = true,
            DefaultValue = "snippingtool",
            Options = [
                new("snippingtool", Loc("Ocr_Source_SnippingTool")),
                new("directory",    Loc("Ocr_Source_Directory")),
                new("url",          Loc("Ocr_Source_Url")),
            ]
        },
        new DirectoryPathSettingDescriptor("directoryPath", Loc("Ocr_EngineDir"))
        {
            Description = Loc("Ocr_EngineDir_Desc"),
        },
        new UrlSettingDescriptor("downloadUrl", Loc("Ocr_DownloadUrl"))
        {
            Description = Loc("Ocr_DownloadUrl_Desc"),
        },
        new DirectoryPathSettingDescriptor("cacheDirectory", Loc("Ocr_CacheDir"))
        {
            Description = Loc("Ocr_CacheDir_Desc"),
            DefaultValue = Path.Combine(Path.GetTempPath(), "Zaya", "OneOcr"),
        },
    ];

    /// <inheritdoc />
    public bool IsAvailable => _engine?.IsAvailable ?? false;

    /// <inheritdoc />
    public async Task InitializeAsync(IReadOnlyDictionary<string, object?>? settings, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_engine is not null)
            return;

        var source = settings?.GetValueOrDefault("source") as string ?? "snippingtool";
        var cacheDir = settings?.GetValueOrDefault("cacheDirectory") as string;

        _engine = source switch
        {
            "snippingtool" => OneOcrEngine.CreateFromSnippingTool(cacheDir),
            "directory" => OneOcrEngine.CreateFromDirectory(
                settings?.GetValueOrDefault("directoryPath") as string
                    ?? throw new ArgumentException("directoryPath is required for 'directory' source")),
            "url" => await OneOcrEngine.CreateFromUrlAsync(
                settings?.GetValueOrDefault("url") as string
                    ?? throw new ArgumentException("url is required for 'url' source"),
                cacheDir, cancellationToken),
            _ => throw new ArgumentException($"Unknown source: {source}")
        };
    }

    /// <inheritdoc />
    public Task<IOCRSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_engine is null)
            throw new InvalidOperationException("OneOCR engine is not initialized. Call InitializeAsync first.");

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IOCRSession>(new OneOcrSession(_engine));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _engine?.Dispose();
        _engine = null;
    }
}
