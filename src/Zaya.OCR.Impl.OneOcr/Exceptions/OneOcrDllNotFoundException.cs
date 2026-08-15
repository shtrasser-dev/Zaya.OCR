using System.Globalization;
using Zaya.OCR.Impl.OneOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Exceptions;

/// <summary>
/// Thrown when <c>oneocr.dll</c> is not found in the specified directory.
/// </summary>
public sealed class OneOcrDllNotFoundException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrDllNotFoundException"/> class.
    /// </summary>
    public OneOcrDllNotFoundException() : base(LocalizationConstants.Exceptions.DllNotFound) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.DllNotFound, culture)
           ?? base.GetLocalizedMessage(culture);
}
