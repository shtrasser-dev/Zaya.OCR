using System.Globalization;
using Zaya.OCR.Impl.WindowsMediaOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.WindowsMediaOcr;

/// <summary>
/// Thrown when the requested OCR language is not installed or not supported by <c>Windows.Media.Ocr</c>.
/// </summary>
public sealed class WindowsMediaOcrLanguageNotSupportedException : LocalizedException
{
    private readonly string _language;

    /// <summary>
    /// Gets the BCP-47 language tag that was requested.
    /// </summary>
    public string Language => _language;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsMediaOcrLanguageNotSupportedException"/> class.
    /// </summary>
    /// <param name="language">The unsupported BCP-47 language tag.</param>
    public WindowsMediaOcrLanguageNotSupportedException(string language)
        : base(LocalizationConstants.Exceptions.LanguageNotSupported)
    {
        _language = language;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.LanguageNotSupported, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _language);
    }
}

/// <summary>
/// Thrown when <c>OcrEngine</c> cannot be created (no language packs / package identity / API failure).
/// </summary>
public sealed class WindowsMediaOcrEngineCreateFailedException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsMediaOcrEngineCreateFailedException"/> class.
    /// </summary>
    public WindowsMediaOcrEngineCreateFailedException()
        : base(LocalizationConstants.Exceptions.EngineCreateFailed) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(
               LocalizationConstants.Exceptions.EngineCreateFailed, culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when the input image pixel format is not supported.
/// </summary>
public sealed class WindowsMediaOcrUnsupportedPixelFormatException : LocalizedException
{
    private readonly string _format;

    /// <summary>
    /// Gets the unsupported pixel format name.
    /// </summary>
    public string FormatName => _format;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsMediaOcrUnsupportedPixelFormatException"/> class.
    /// </summary>
    /// <param name="format">The unsupported format display name.</param>
    public WindowsMediaOcrUnsupportedPixelFormatException(string format)
        : base(LocalizationConstants.Exceptions.UnsupportedPixelFormat)
    {
        _format = format;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.UnsupportedPixelFormat, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _format);
    }
}

/// <summary>
/// Thrown when <c>RecognizeAsync</c> fails.
/// </summary>
public sealed class WindowsMediaOcrRecognizeFailedException : LocalizedException
{
    private readonly string _detail;

    /// <summary>
    /// Gets the failure detail.
    /// </summary>
    public string Detail => _detail;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsMediaOcrRecognizeFailedException"/> class.
    /// </summary>
    /// <param name="detail">Technical detail from the failure.</param>
    public WindowsMediaOcrRecognizeFailedException(string detail)
        : base(LocalizationConstants.Exceptions.RecognizeFailed)
    {
        _detail = detail;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(
                         LocalizationConstants.Exceptions.RecognizeFailed, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _detail);
    }
}

/// <summary>
/// Thrown when Windows.Media.Ocr is not available on the current system.
/// </summary>
public sealed class WindowsMediaOcrNotAvailableException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsMediaOcrNotAvailableException"/> class.
    /// </summary>
    public WindowsMediaOcrNotAvailableException()
        : base(LocalizationConstants.Exceptions.NotAvailable) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(
               LocalizationConstants.Exceptions.NotAvailable, culture)
           ?? base.GetLocalizedMessage(culture);
}
