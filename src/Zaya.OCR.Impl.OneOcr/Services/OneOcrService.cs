using Zaya.OCR.Impl.OneOcr.Constants;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Services;

/// <summary>
/// OneOCR engine implementation of <see cref="IOCRService"/> using P/Invoke
/// into the native <c>oneocr.dll</c> (Windows 11 SnippingTool OCR engine).
/// No WinRT, no Windows App SDK, no package identity required.
/// Pass engine settings directly to <see cref="CreateSessionAsync(IReadOnlyDictionary{string, object}, CancellationToken)"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EngineId"/> is <c>oneocr</c>.
/// Settings are exposed via <see cref="Settings"/> as <see cref="SettingDescriptor"/> instances
/// for UI hosts, and accepted as a string-keyed dictionary by
/// <see cref="CreateSessionAsync(IReadOnlyDictionary{string, object}, CancellationToken)"/>.
/// Use <see cref="OneOcrConfig"/> for a typed alternative.
/// </para>
/// <para><b>Setting keys</b></para>
/// <list type="table">
/// <listheader>
/// <term>Key</term>
/// <term>Type</term>
/// <term>Default</term>
/// <term>Description</term>
/// </listheader>
/// <item>
/// <term><c>source</c></term>
/// <term>enum string</term>
/// <term><c>auto</c></term>
/// <term>
/// Where to load engine files from.
/// Values: <c>auto</c> (SnippingTool, then URL fallback),
/// <c>snippingtool</c>, <c>directory</c>, <c>url</c>.
/// </term>
/// </item>
/// <item>
/// <term><c>directoryPath</c></term>
/// <term>directory path</term>
/// <term>(none)</term>
/// <term>
/// Local folder with <c>oneocr.dll</c>, <c>onnxruntime.dll</c>, and <c>oneocr.onemodel</c>.
/// Required when <c>source</c> is <c>directory</c>.
/// </term>
/// </item>
/// <item>
/// <term><c>downloadUrl</c></term>
/// <term>URL</term>
/// <term>GitHub <c>Zaya.External</c> OneOCR.zip release</term>
/// <term>
/// Package URL used when <c>source</c> is <c>url</c>,
/// and as fallback when <c>source</c> is <c>auto</c>.
/// </term>
/// </item>
/// <item>
/// <term><c>cacheDirectory</c></term>
/// <term>directory path</term>
/// <term><c>%TEMP%\Zaya\OneOcr</c></term>
/// <term>
/// Writable cache for copied/extracted engine files
/// (<c>auto</c>, <c>snippingtool</c>, <c>url</c>).
/// Hosts such as ScreenTranslator may override this key.
/// </term>
/// </item>
/// <item>
/// <term><c>minConfidence</c></term>
/// <term>integer 0–100</term>
/// <term><c>90</c></term>
/// <term>Minimum word confidence (percent). Words below the threshold are dropped.</term>
/// </item>
/// </list>
/// <para>
/// Example:
/// </para>
/// <code language="csharp">
/// using var ocr = new OneOcrService();
/// using var session = await ocr.CreateSessionAsync(new Dictionary&lt;string, object&gt;
/// {
///     ["source"] = "auto",
///     ["minConfidence"] = 50,
/// });
/// var result = await session.RecognizeAsync(image);
/// </code>
/// </remarks>
public sealed class OneOcrService : IOCRService
{
    private const string EngineIdValue = "oneocr";
    private const string DefaultCacheCompany = "Zaya";
    private const string DefaultCacheProduct = "OneOcr";

    private const string DefaultDownloadUrl = @"https://github.com/shtrasser-dev/Zaya.External/releases/latest/download/OneOCR.zip";

    private static IReadOnlyList<SettingDescriptor> _settings = [
        new EnumSettingDescriptor(SettingsConstants.Source, Loc(LocalizationConstants.Settings.Source))
        {
            Description = Loc(LocalizationConstants.Settings.Source_Desc),
            IsRequired = static _ => true,
            DefaultValue = SettingsConstants.Auto,
            Options = [
                new(SettingsConstants.Auto,         Loc(LocalizationConstants.Settings.Source_Auto)),
                new(SettingsConstants.SnippingTool, Loc(LocalizationConstants.Settings.Source_SnippingTool)),
                new(SettingsConstants.Directory,    Loc(LocalizationConstants.Settings.Source_Directory)),
                new(SettingsConstants.Url,          Loc(LocalizationConstants.Settings.Source_Url)),
            ]
        },
        new DirectoryPathSettingDescriptor(SettingsConstants.DirectoryPath, Loc(LocalizationConstants.Settings.EngineDir))
        {
            Description = Loc(LocalizationConstants.Settings.EngineDir_Desc),
            IsVisible  = s => s.GetValueOrDefault(SettingsConstants.Source) as string == SettingsConstants.Directory,
            IsRequired = s => s.GetValueOrDefault(SettingsConstants.Source) as string == SettingsConstants.Directory,
        },
        new UrlSettingDescriptor(SettingsConstants.DownloadUrl, Loc(LocalizationConstants.Settings.DownloadUrl))
        {
            Description = Loc(LocalizationConstants.Settings.DownloadUrl_Desc),
            DefaultValue = DefaultDownloadUrl,
            IsVisible  = s => (s.GetValueOrDefault(SettingsConstants.Source) as string ?? SettingsConstants.Auto) is SettingsConstants.Url,
            IsRequired = s => s.GetValueOrDefault(SettingsConstants.Source) as string == SettingsConstants.Url,
        },
        new DirectoryPathSettingDescriptor(SettingsConstants.CacheDirectory, Loc(LocalizationConstants.Settings.CacheDir))
        {
            Description = Loc(LocalizationConstants.Settings.CacheDir_Desc),
            DefaultValue = Path.Combine(Path.GetTempPath(), DefaultCacheCompany, DefaultCacheProduct),
            IsVisible  = s => (s.GetValueOrDefault(SettingsConstants.Source) as string ?? SettingsConstants.Auto) is SettingsConstants.Auto or SettingsConstants.SnippingTool or SettingsConstants.Url,
        },
        new IntegerSettingDescriptor(SettingsConstants.MinConfidence, Loc(LocalizationConstants.Settings.MinConfidence))
        {
            Description = Loc(LocalizationConstants.Settings.MinConfidence_Desc),
            DefaultValue = 70,
            MinValue = 0,
            MaxValue = 100,
        },
    ];

    private bool _disposed;

    private static LocalizedString Loc(string key)
        => new(key, culture => Properties.Resources.ResourceManager.GetString(key, culture)!);

    /// <inheritdoc />
    public string EngineId => EngineIdValue;

    /// <inheritdoc />
    public LocalizedString DisplayName => Loc(LocalizationConstants.Settings.EngineName);

    /// <inheritdoc />
    public LocalizedString Description => Loc(LocalizationConstants.Settings.EngineDesc);

    /// <inheritdoc />
    public PixelFormat PreferredPixelFormat => PixelFormat.Bgra32;

    /// <summary>
    /// Gets the engine-specific settings declared by OneOCR
    /// (<c>source</c>, <c>directoryPath</c>, <c>downloadUrl</c>, <c>cacheDirectory</c>, <c>minConfidence</c>).
    /// </summary>
    /// <remarks>
    /// See the type-level remarks on <see cref="OneOcrService"/> for key names, defaults, and visibility rules.
    /// </remarks>
    public IReadOnlyList<SettingDescriptor> Settings { get; } = _settings;

    /// <inheritdoc />
    public bool IsAvailable => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    /// <inheritdoc />
    public async Task<IOCRSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var settingDescriptorList = new SettingDescriptorList(_settings);
        return await CreateSessionAsync(settingDescriptorList, cancellationToken);
    }

    /// <summary>
    /// Creates a new OCR session with the specified engine settings.
    /// </summary>
    /// <param name="settings">
    /// Dictionary of setting keys (see type-level remarks). Missing keys use descriptor defaults.
    /// </param>
    /// <param name="cancellationToken">Token to cancel session creation (e.g. while downloading).</param>
    /// <returns>An active OCR session ready to recognize text.</returns>
    public async Task<IOCRSession> CreateSessionAsync(IReadOnlyDictionary<string, object> settings, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var settingDescriptorList = new SettingDescriptorList(_settings);
        settingDescriptorList.Bind(settings);
        return await CreateSessionAsync(settingDescriptorList, cancellationToken);
    }

    /// <inheritdoc />
    private async Task<IOCRSession> CreateSessionAsync(SettingDescriptorList settingDescriptorList, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var source = settingDescriptorList.GetValueAsString(SettingsConstants.Source);
        var cacheDir = settingDescriptorList.GetValueAsString(SettingsConstants.CacheDirectory);

        var engine = source switch
        {
            SettingsConstants.Auto => await CreateEngineFromAutoAsync(
                RequireDownloadUrl(settingDescriptorList),
                cacheDir, cancellationToken),
            SettingsConstants.SnippingTool => OneOcrEngine.CreateFromSnippingTool(cacheDir),
            SettingsConstants.Directory => OneOcrEngine.CreateFromDirectory(
                RequireDirectoryPath(settingDescriptorList)),
            SettingsConstants.Url => await OneOcrEngine.CreateFromUrlAsync(
                RequireDownloadUrl(settingDescriptorList),
                cacheDir, cancellationToken),
            _ => throw new OneOcrUnknownSourceException(source)
        };

        var minConfidence = settingDescriptorList.GetValueAsInt(SettingsConstants.MinConfidence) / 100.0;
        cancellationToken.ThrowIfCancellationRequested();
        return new OneOcrSession(engine, minConfidence);
    }

    private static string RequireDirectoryPath(SettingDescriptorList settings)
    {
        var path = settings.GetValueAsString(SettingsConstants.DirectoryPath);
        if (string.IsNullOrWhiteSpace(path))
            throw new OneOcrDirectoryPathRequiredException();
        return path;
    }

    private static string RequireDownloadUrl(SettingDescriptorList settings)
    {
        var url = settings.GetValueAsString(SettingsConstants.DownloadUrl);
        if (string.IsNullOrWhiteSpace(url))
            throw new OneOcrDownloadUrlRequiredException();
        return url;
    }

    private static async Task<OneOcrEngine> CreateEngineFromAutoAsync(
        string downloadUrl, string? cacheDir, CancellationToken cancellationToken)
    {
        try
        {
            return OneOcrEngine.CreateFromSnippingTool(cacheDir);
        }
        catch (OneOcrSnippingToolNotFoundException)
        {
            return await OneOcrEngine.CreateFromUrlAsync(downloadUrl, cacheDir, cancellationToken);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
