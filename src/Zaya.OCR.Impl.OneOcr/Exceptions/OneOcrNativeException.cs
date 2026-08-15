using System.Globalization;
using Zaya.OCR.Impl.OneOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Exceptions;

/// <summary>
/// Thrown when a native OneOCR API call fails (init, pipeline, or recognition).
/// </summary>
public sealed class OneOcrNativeException : LocalizedException
{
    private readonly string _detail;

    /// <summary>
    /// Gets the native failure detail (API name and/or status code).
    /// </summary>
    public string Detail => _detail;

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrNativeException"/> class.
    /// </summary>
    /// <param name="detail">Technical detail from the native call (e.g. <c>CreateOcrPipeline: 0x6</c>).</param>
    public OneOcrNativeException(string detail) : base(LocalizationConstants.Exceptions.NativeFailed)
    {
        _detail = detail;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.NativeFailed, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _detail);
    }
}
