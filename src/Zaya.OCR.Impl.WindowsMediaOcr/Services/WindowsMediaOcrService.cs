using Windows.Globalization;
using Windows.Media.Ocr;
using Zaya.OCR.Impl.WindowsMediaOcr.Constants;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Services;

/// <summary>
/// Windows.Media.Ocr implementation of <see cref="IOCRService"/>.
/// Uses the official WinRT OCR API available on Windows 10+.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EngineId"/> is <c>windows-media-ocr</c>.
/// Desktop apps typically require <b>package identity</b> (MSIX) to call this API.
/// Install OCR language packs in Windows Settings if recognition languages are missing.
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
/// <term><c>language</c></term>
/// <term>enum string</term>
/// <term><c>auto</c></term>
/// <term>
/// <c>auto</c> uses <c>OcrEngine.TryCreateFromUserProfileLanguages</c>;
/// otherwise a BCP-47 tag passed to <c>TryCreateFromLanguage</c>.
/// Options include installed OCR languages when available, else <see cref="Languages.All"/>.
/// </term>
/// </item>
/// </list>
/// <para>
/// Example:
/// </para>
/// <code language="csharp">
/// using var ocr = new WindowsMediaOcrService();
/// using var session = await ocr.CreateSessionAsync(new Dictionary&lt;string, object&gt;
/// {
///     ["language"] = "en",
/// });
/// var result = await session.RecognizeAsync(image);
/// </code>
/// </remarks>
public sealed class WindowsMediaOcrService : IOCRService
{
    private const string EngineIdValue = "windows-media-ocr";

    private readonly IReadOnlyList<SettingDescriptor> _settings;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsMediaOcrService"/> class.
    /// </summary>
    public WindowsMediaOcrService()
    {
        _settings = BuildSettings();
    }

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

    /// <inheritdoc />
    public IReadOnlyList<SettingDescriptor> Settings => _settings;

    /// <inheritdoc />
    public bool IsAvailable
    {
        get
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10))
                return false;

            try
            {
                if (OcrEngine.AvailableRecognizerLanguages.Count > 0)
                    return true;
                return OcrEngine.TryCreateFromUserProfileLanguages() is not null;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc />
    public Task<IOCRSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var settingDescriptorList = new SettingDescriptorList(_settings);
        return CreateSessionAsync(settingDescriptorList, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IOCRSession> CreateSessionAsync(
        IReadOnlyDictionary<string, object> settings,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var settingDescriptorList = new SettingDescriptorList(_settings);
        settingDescriptorList.Bind(settings);
        return CreateSessionAsync(settingDescriptorList, cancellationToken);
    }

    private Task<IOCRSession> CreateSessionAsync(
        SettingDescriptorList settingDescriptorList,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
            throw new WindowsMediaOcrNotAvailableException();

        var languageTag = settingDescriptorList.GetValueAsString(SettingsConstants.Language);
        if (string.IsNullOrWhiteSpace(languageTag))
            languageTag = SettingsConstants.Auto;

        OcrEngine? engine;
        try
        {
            if (string.Equals(languageTag, SettingsConstants.Auto, StringComparison.OrdinalIgnoreCase))
            {
                engine = OcrEngine.TryCreateFromUserProfileLanguages();
            }
            else
            {
                var language = new Language(languageTag);
                if (!OcrEngine.IsLanguageSupported(language))
                    throw new WindowsMediaOcrLanguageNotSupportedException(languageTag);

                engine = OcrEngine.TryCreateFromLanguage(language);
            }
        }
        catch (WindowsMediaOcrLanguageNotSupportedException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new WindowsMediaOcrEngineCreateFailedException();
        }

        if (engine is null)
        {
            if (!string.Equals(languageTag, SettingsConstants.Auto, StringComparison.OrdinalIgnoreCase))
                throw new WindowsMediaOcrLanguageNotSupportedException(languageTag);
            throw new WindowsMediaOcrEngineCreateFailedException();
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IOCRSession>(new WindowsMediaOcrSession(engine));
    }

    private static IReadOnlyList<SettingDescriptor> BuildSettings()
    {
        var options = BuildLanguageOptions();
        return
        [
            new EnumSettingDescriptor(SettingsConstants.Language, Loc(LocalizationConstants.Settings.Language))
            {
                Description = Loc(LocalizationConstants.Settings.Language_Desc),
                IsRequired = static _ => true,
                DefaultValue = SettingsConstants.Auto,
                Options = options,
            },
        ];
    }

    private static IReadOnlyList<EnumOption> BuildLanguageOptions()
    {
        var options = new List<EnumOption>
        {
            new(SettingsConstants.Auto, Loc(LocalizationConstants.Settings.Language_Auto)),
        };

        try
        {
            foreach (var lang in OcrEngine.AvailableRecognizerLanguages
                         .OrderBy(l => l.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                var tag = lang.LanguageTag;
                var displayName = lang.DisplayName;
                options.Add(new EnumOption(tag, new LocalizedString(tag, _ => displayName)));
            }
        }
        catch
        {
            // WinRT may fail without package identity; fall back to the shared language list.
            foreach (var option in Languages.All)
                options.Add(option);
        }

        // Ensure at least auto + common languages when the OS reports none.
        if (options.Count == 1)
        {
            foreach (var option in Languages.All)
                options.Add(option);
        }

        return options;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
