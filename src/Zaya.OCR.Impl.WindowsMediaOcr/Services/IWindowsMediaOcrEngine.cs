using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Services;

/// <summary>
/// WinRT <see cref="OcrEngine"/> wrapper used by Windows.Media.Ocr sessions.
/// </summary>
internal interface IWindowsMediaOcrEngine
{
    /// <summary>
    /// Recognizes text in a <see cref="SoftwareBitmap"/>.
    /// </summary>
    Task<OcrResult> RecognizeAsync(SoftwareBitmap bitmap, CancellationToken cancellationToken = default);
}
