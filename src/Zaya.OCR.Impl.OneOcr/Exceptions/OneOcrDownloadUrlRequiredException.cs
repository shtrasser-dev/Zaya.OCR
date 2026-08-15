using System.Globalization;
using Zaya.OCR.Impl.OneOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Exceptions;

/// <summary>
/// Thrown when the <c>downloadUrl</c> setting is required but not provided (source is <c>url</c>).
/// </summary>
public sealed class OneOcrDownloadUrlRequiredException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrDownloadUrlRequiredException"/> class.
    /// </summary>
    public OneOcrDownloadUrlRequiredException() : base(LocalizationConstants.Exceptions.DownloadUrlRequired) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.DownloadUrlRequired, culture)
           ?? base.GetLocalizedMessage(culture);
}
