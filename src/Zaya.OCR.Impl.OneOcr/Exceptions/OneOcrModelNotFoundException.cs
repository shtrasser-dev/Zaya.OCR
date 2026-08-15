using System.Globalization;
using Zaya.OCR.Impl.OneOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Exceptions;

/// <summary>
/// Thrown when the <c>oneocr.onemodel</c> file is not found.
/// </summary>
public sealed class OneOcrModelNotFoundException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrModelNotFoundException"/> class.
    /// </summary>
    public OneOcrModelNotFoundException() : base(LocalizationConstants.Exceptions.ModelNotFound) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.ModelNotFound, culture)
           ?? base.GetLocalizedMessage(culture);
}
