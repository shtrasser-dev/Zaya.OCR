using System.Globalization;
using Zaya.OCR.Impl.OneOcr.Constants;
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
    public OneOcrSnippingToolNotFoundException() : base(LocalizationConstants.Exceptions.SnippingToolNotFound) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.SnippingToolNotFound, culture)
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
    public OneOcrModelNotFoundException() : base(LocalizationConstants.Exceptions.ModelNotFound) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.ModelNotFound, culture)
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
    public OneOcrDllLoadException() : base(LocalizationConstants.Exceptions.DllLoadFailed) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.DllLoadFailed, culture)
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
    public OneOcrDllNotFoundException() : base(LocalizationConstants.Exceptions.DllNotFound) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.DllNotFound, culture)
           ?? base.GetLocalizedMessage(culture);
}

/// <summary>
/// Thrown when the <c>directoryPath</c> setting is required but not provided (source is <c>Directory</c>).
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

/// <summary>
/// Thrown when the <c>downloadUrl</c> setting is required but not provided (source is <c>Url</c>).
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

/// <summary>
/// Thrown when an unknown source value is provided to <see cref="Services.OneOcrService"/>.
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

/// <summary>
/// Thrown if <see cref="Services.OneOcrService"/> fails to initialize.
/// </summary>
public sealed class OneOcrNotInitializedException : LocalizedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OneOcrNotInitializedException"/> class.
    /// </summary>
    public OneOcrNotInitializedException() : base(LocalizationConstants.Exceptions.NotInitialized) { }

    /// <inheritdoc />
    public override string GetLocalizedMessage(CultureInfo culture)
        => Properties.Resources.ResourceManager.GetString(LocalizationConstants.Exceptions.NotInitialized, culture)
           ?? base.GetLocalizedMessage(culture);
}
