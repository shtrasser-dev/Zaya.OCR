namespace Zaya.OCR.Impl.OneOcr.Models;

/// <summary>
/// Specifies how the OneOCR engine files are obtained.
/// </summary>
public enum OneOcrSource
{
    /// <summary>
    /// Try SnippingTool first; if not found, download from URL.
    /// Dictionary value: <c>auto</c>.
    /// </summary>
    Auto,

    /// <summary>
    /// Auto-detect from the Windows 11 SnippingTool installation.
    /// Dictionary value: <c>snippingtool</c>.
    /// </summary>
    SnippingTool,

    /// <summary>
    /// Load from a local directory containing oneocr.dll, onnxruntime.dll, and oneocr.onemodel.
    /// Dictionary value: <c>directory</c>.
    /// </summary>
    Directory,

    /// <summary>
    /// Download from a URL.
    /// Dictionary value: <c>url</c>.
    /// </summary>
    Url
}
