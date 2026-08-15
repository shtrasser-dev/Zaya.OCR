using System.Globalization;
using Zaya.OCR.Impl.WindowsMediaOcr.Constants;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Exceptions;

/// <summary>
/// Thrown when <c>OcrEngine</c> cannot be created (no language packs / package identity / API failure).
/// </summary>
public sealed class WindowsMediaOcrEngineCreateFailedException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsMediaOcrEngineCreateFailedException"/> class.
    /// </summary>
    public WindowsMediaOcrEngineCreateFailedException()
        : base(LocalizationConstants.Exceptions.EngineCreateFailed) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(
               LocalizationConstants.Exceptions.EngineCreateFailed, culture)
           ?? base.GetLocalizedMessage(culture);
}
