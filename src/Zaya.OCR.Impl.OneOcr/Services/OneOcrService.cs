using Zaya.OCR.Impl.OneOcr.Models;
using Zaya.OCR.Models;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Services;

/// <summary>
/// OneOCR engine implementation of <see cref="IOCRService"/> using P/Invoke
/// into the native oneocr.dll (Windows 11 SnippingTool OCR engine).
/// No WinRT, no Windows App SDK, no package identity required.
/// </summary>
public sealed class OneOcrService : IOCRService
{
    /// <inheritdoc />
    public string EngineId => "oneocr";

    /// <inheritdoc />
    public PixelFormat PreferredPixelFormat => PixelFormat.Bgra32;

    /// <inheritdoc />
    public bool IsAvailable => OneOcrNative.IsAvailable;

    /// <inheritdoc />
    public Task<IOCRSession> CreateSessionAsync(OcrOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IOCRSession>(new OneOcrSession(options ?? new OcrOptions()));
    }
}
