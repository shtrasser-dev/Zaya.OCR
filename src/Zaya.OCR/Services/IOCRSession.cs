using Zaya.OCR.Models;
using Zaya.Primitives;

namespace Zaya.OCR.Services;

/// <summary>
/// Represents an active OCR session with fixed recognition options.
/// Create via <see cref="IOCRService.CreateSessionAsync"/>.
/// </summary>
public interface IOCRSession : IDisposable
{
    /// <summary>
    /// Recognizes text from the provided raw image.
    /// </summary>
    /// <param name="image">The raw image to recognize text from.</param>
    /// <param name="cancellationToken">Token to cancel the recognition operation.</param>
    /// <returns>The OCR result containing recognized words and confidence.</returns>
    Task<IOCRResult> RecognizeAsync(IRawImage image, CancellationToken cancellationToken = default);
}
