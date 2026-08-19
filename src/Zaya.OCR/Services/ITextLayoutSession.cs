using Zaya.Primitives.OCR;

namespace Zaya.OCR.Services;

/// <summary>
/// Represents an active text layout session with fixed processing parameters.
/// Create via <see cref="ITextLayoutService.CreateSessionAsync(IReadOnlyDictionary{string, object}, CancellationToken)"/>.
/// </summary>
public interface ITextLayoutSession : IDisposable
{
    /// <summary>
    /// Processes an OCR result and merges individual words into structured text blocks.
    /// </summary>
    /// <param name="result">The raw OCR result containing recognized words.</param>
    /// <param name="cancellationToken">Token to cancel the processing operation.</param>
    /// <returns>The processed text result containing ordered text blocks.</returns>
    Task<ITextResult> ProcessAsync(IOCRResult result, CancellationToken cancellationToken = default);
}
