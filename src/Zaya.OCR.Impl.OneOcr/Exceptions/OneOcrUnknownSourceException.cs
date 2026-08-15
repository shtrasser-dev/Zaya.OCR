using System.Globalization;
using Zaya.OCR.Impl.OneOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Exceptions;

/// <summary>
/// Thrown when an unknown source value is provided to <see cref="OneOcrService"/>.
/// </summary>
public sealed class OneOcrUnknownSourceException : LocalizedException
{
    private readonly string _source;

    /// <summary>
    /// Gets the unknown source value that caused the exception.
    /// </summary>
    public string SourceValue => _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrUnknownSourceException"/> class.
    /// </summary>
    /// <param name="source">The unrecognized source value.</param>
    public OneOcrUnknownSourceException(string source) : base(LocalizationConstants.Exceptions.UnknownSource)
    {
        _source = source;
    }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
    {
        var format = Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.UnknownSource, culture)
                     ?? base.GetLocalizedMessage(culture);
        return string.Format(format, _source);
    }
}
