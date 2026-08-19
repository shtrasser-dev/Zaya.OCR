using Zaya.OCR.Impl.OneOcr.Models;
using Zaya.OCR.Impl.OneOcr.Services;
using Zaya.OCR.Services;
using Zaya.Primitives;
using Zaya.Primitives.OCR;

namespace Zaya.OCR.Impl.OneOcr.Services.Impl;

/// <summary>
/// OneOCR session that performs OCR using the native <c>oneocr.dll</c> via P/Invoke.
/// Owns the underlying <see cref="IOneOcrEngine"/> and disposes it with the session.
/// </summary>
public sealed class OneOcrSession : IOCRSession
{
    private readonly IOneOcrEngine _engine;
    private readonly double _minConfidence;
    private bool _disposed;

    internal OneOcrSession(IOneOcrEngine engine, double minConfidence = 0)
    {
        _engine = engine;
        _minConfidence = minConfidence;
    }

    /// <inheritdoc />
    public Task<IOCRResult> RecognizeAsync(IRawImage image, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var pixelData = image.ToByteArray();
        var nativeWords = _engine.Recognize(pixelData, image.Width, image.Height, image.Stride, _minConfidence);

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
        _engine.Dispose();
    }
}
