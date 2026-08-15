using System.Globalization;
using Zaya.OCR.Impl.WindowsMediaOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Exceptions;

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
