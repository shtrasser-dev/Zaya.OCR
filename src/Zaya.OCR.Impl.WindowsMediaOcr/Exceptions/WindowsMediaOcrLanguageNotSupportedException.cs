using System.Globalization;
using Zaya.OCR.Impl.WindowsMediaOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Exceptions;

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
