using Zaya.OCR.Models;

namespace Zaya.OCR.Services;

/// <summary>
/// Represents an active OCR session with fixed recognition options.
/// Create via <see cref="IOCRService.CreateSessionAsync"/>.
/// </summary>
public interface IOCRSession : IDisposable
{
    /// <summary>
    /// Gets the options configured for this session.
    /// </summary>
    OcrOptions Options { get; }

    /// <summary>
    /// Recognizes text from the provided image data.
    /// </summary>
    /// <param name="data">Raw image bytes (e.g., PNG, JPEG, BMP).</param>
    /// <param name="cancellationToken">Token to cancel the recognition operation.</param>
    /// <returns>The OCR result containing recognized words and confidence.</returns>
    Task<IOCRResult> RecognizeAsync(byte[] data, CancellationToken cancellationToken = default);
}
