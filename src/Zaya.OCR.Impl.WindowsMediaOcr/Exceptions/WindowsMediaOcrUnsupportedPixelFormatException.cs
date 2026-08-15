using System.Globalization;
using Zaya.OCR.Impl.WindowsMediaOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Exceptions;

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
