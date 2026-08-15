using System.Globalization;
using Zaya.OCR.Impl.WindowsMediaOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Exceptions;

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
