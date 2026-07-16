using System.Globalization;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr;

/// <summary>
/// Thrown when the Windows 11 SnippingTool installation is not found on the current system.
/// </summary>
public sealed class OneOcrSnippingToolNotFoundException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrSnippingToolNotFoundException"/> class.
    /// </summary>
    public OneOcrSnippingToolNotFoundException() : base("Ocr_Err_SnippingToolNotFound") { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString("Ocr_Err_SnippingToolNotFound", culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when the <c>oneocr.onemodel</c> file is not found.
/// </summary>
public sealed class OneOcrModelNotFoundException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrModelNotFoundException"/> class.
    /// </summary>
    public OneOcrModelNotFoundException() : base("Ocr_Err_ModelNotFound") { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString("Ocr_Err_ModelNotFound", culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when <c>oneocr.dll</c> fails to load via <c>LoadLibraryEx</c>.
/// </summary>
public sealed class OneOcrDllLoadException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrDllLoadException"/> class.
    /// </summary>
    public OneOcrDllLoadException() : base("Ocr_Err_DllLoadFailed") { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString("Ocr_Err_DllLoadFailed", culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when <c>oneocr.dll</c> is not found in the specified directory.
/// </summary>
public sealed class OneOcrDllNotFoundException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrDllNotFoundException"/> class.
    /// </summary>
    public OneOcrDllNotFoundException() : base("Ocr_Err_DllNotFound") { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString("Ocr_Err_DllNotFound", culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when the URL-based engine download source is selected but not yet implemented.
/// </summary>
public sealed class OneOcrDownloadNotImplementedException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrDownloadNotImplementedException"/> class.
    /// </summary>
    public OneOcrDownloadNotImplementedException() : base("Ocr_Err_DownloadNotImplemented") { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString("Ocr_Err_DownloadNotImplemented", culture)
           ?? base.GetLocalizedMessage(culture);
}
