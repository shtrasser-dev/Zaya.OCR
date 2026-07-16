using Zaya.OCR.Impl.OneOcr.Models;
using Zaya.OCR.Models;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr.Services;

/// <summary>
/// OneOCR session that performs OCR using the native <c>oneocr.dll</c> via P/Invoke.
/// </summary>
public sealed class OneOcrSession : IOCRSession
{
    private readonly OneOcrEngine _engine;
    private bool _disposed;

    internal OneOcrSession(OneOcrEngine engine)
    {
        _engine = engine;
    }

    /// <inheritdoc />
    public Task<IOCRResult> RecognizeAsync(IRawImage image, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pixelData = image.ToByteArray();
        var nativeWords = _engine.Recognize(pixelData, image.Width, image.Height, image.Stride);

        var words = nativeWords
            .Select(w => (IOCRWord)new OneOcrWord(w.Text, w.Bounds, w.Confidence))
            .ToList();

        var overallConfidence = words.Count > 0
            ? words.Average(w => w.Confidence)
            : 0.0;

        return Task.FromResult<IOCRResult>(new OneOcrResult(words, overallConfidence));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
