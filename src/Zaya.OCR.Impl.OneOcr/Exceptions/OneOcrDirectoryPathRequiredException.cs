using System.Globalization;
using Zaya.OCR.Impl.OneOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Exceptions;

/// <summary>
/// Thrown when the <c>directoryPath</c> setting is required but not provided (source is <c>directory</c>).
/// </summary>
public sealed class OneOcrDirectoryPathRequiredException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrDirectoryPathRequiredException"/> class.
    /// </summary>
    public OneOcrDirectoryPathRequiredException() : base(LocalizationConstants.Exceptions.DirectoryPathRequired) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.DirectoryPathRequired, culture)
           ?? base.GetLocalizedMessage(culture);
}
