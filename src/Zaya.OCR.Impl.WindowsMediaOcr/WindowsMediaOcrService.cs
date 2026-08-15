using Windows.Globalization;
using Windows.Media.Ocr;
using Zaya.Logging.Services;
using Zaya.OCR.Impl.WindowsMediaOcr.Constants;
using Zaya.OCR.Impl.WindowsMediaOcr.Exceptions;
using Zaya.OCR.Impl.WindowsMediaOcr.Services;
using Zaya.OCR.Impl.WindowsMediaOcr.Services.Impl;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.WindowsMediaOcr;

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
    private readonly ILoggingWrapper _loggingWrapper;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance using <see cref="EmptyLoggingWrapper.Instance"/>.
    /// </summary>
    public WindowsMediaOcrService() : this(EmptyLoggingWrapper.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified logging wrapper.
    /// </summary>
    /// <param name="loggingWrapper">Logging wrapper used when creating sessions.</param>
    public WindowsMediaOcrService(ILoggingWrapper loggingWrapper)
    {
        _loggingWrapper = loggingWrapper;
        _settings = SettingsDescriptorsConstants.Create();
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

        OcrEngine? nativeEngine;
        try
        {
            if (string.Equals(languageTag, SettingsConstants.Auto, StringComparison.OrdinalIgnoreCase))
            {
                nativeEngine = OcrEngine.TryCreateFromUserProfileLanguages();
            }
            else
            {
                var language = new Language(languageTag);
                if (!OcrEngine.IsLanguageSupported(language))
                    throw new WindowsMediaOcrLanguageNotSupportedException(languageTag);

                nativeEngine = OcrEngine.TryCreateFromLanguage(language);
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

        if (nativeEngine is null)
        {
            if (!string.Equals(languageTag, SettingsConstants.Auto, StringComparison.OrdinalIgnoreCase))
                throw new WindowsMediaOcrLanguageNotSupportedException(languageTag);
            throw new WindowsMediaOcrEngineCreateFailedException();
        }

        cancellationToken.ThrowIfCancellationRequested();
        var wrappedEngine = _loggingWrapper.Wrap<IWindowsMediaOcrEngine>(new WindowsMediaOcrEngine(nativeEngine));
        return Task.FromResult(_loggingWrapper.Wrap<IOCRSession>(new WindowsMediaOcrSession(wrappedEngine)));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
