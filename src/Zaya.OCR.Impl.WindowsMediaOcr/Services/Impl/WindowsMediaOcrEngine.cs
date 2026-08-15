using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Zaya.OCR.Impl.WindowsMediaOcr.Services;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Services.Impl;

internal sealed class WindowsMediaOcrEngine : IWindowsMediaOcrEngine
{
    private readonly OcrEngine _engine;

    public WindowsMediaOcrEngine(OcrEngine engine)
    {
        _engine = engine;
    }
    public Task<OcrResult> RecognizeAsync(SoftwareBitmap bitmap, CancellationToken cancellationToken = default)
        => _engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
}
