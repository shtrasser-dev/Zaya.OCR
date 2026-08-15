using System.Globalization;
using Zaya.OCR.Impl.OneOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Exceptions;

/// <summary>
/// Thrown when the Windows 11 SnippingTool installation is not found on the current system
/// and no usable engine files are present in the cache directory.
/// </summary>
public sealed class OneOcrSnippingToolNotFoundException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrSnippingToolNotFoundException"/> class.
    /// </summary>
    public OneOcrSnippingToolNotFoundException() : base(LocalizationConstants.Exceptions.SnippingToolNotFound) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.SnippingToolNotFound, culture)
           ?? base.GetLocalizedMessage(culture);
}
