using System.Globalization;
using Zaya.OCR.Impl.OneOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Exceptions;

/// <summary>
/// Thrown when <c>oneocr.dll</c> fails to load via <c>LoadLibraryEx</c>.
/// </summary>
public sealed class OneOcrDllLoadException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrDllLoadException"/> class.
    /// </summary>
    public OneOcrDllLoadException() : base(LocalizationConstants.Exceptions.DllLoadFailed) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.DllLoadFailed, culture)
           ?? base.GetLocalizedMessage(culture);
}
