using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Zaya.OCR.Impl.WindowsMediaOcr.Models;
using Zaya.OCR.Models;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Services;

/// <summary>
/// OCR session backed by <see cref="OcrEngine"/> (<c>Windows.Media.Ocr</c>).
/// </summary>
public sealed class WindowsMediaOcrSession : IOCRSession
{
    private readonly OcrEngine _engine;
    private bool _disposed;

    internal WindowsMediaOcrSession(OcrEngine engine)
    {
        _engine = engine;
    }

    /// <inheritdoc />
    public async Task<IOCRResult> RecognizeAsync(IRawImage image, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        if (image.Format != PixelFormat.Bgra32)
            throw new WindowsMediaOcrUnsupportedPixelFormatException(image.Format.Name ?? "unknown");

        using var softwareBitmap = CreateSoftwareBitmap(image);
        cancellationToken.ThrowIfCancellationRequested();

        OcrResult ocrResult;
        try
        {
            ocrResult = await _engine.RecognizeAsync(softwareBitmap).AsTask(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new WindowsMediaOcrRecognizeFailedException(ex.Message);
        }

        var textAngle = ocrResult.TextAngle;
        var imageWidth = (float)image.Width;
        var imageHeight = (float)image.Height;

        var words = new List<IOCRWord>();
        foreach (var line in ocrResult.Lines)
        {
            foreach (var word in line.Words)
            {
                var bounds = WindowsMediaOcrBounds.FromWordRect(
                    word.BoundingRect,
                    textAngle,
                    imageWidth,
                    imageHeight);

                // WinRT OcrWord has no confidence; treat detections as fully confident.
                words.Add(new WindowsMediaOcrWord(word.Text, bounds, 1.0));
            }
        }

        var overallConfidence = words.Count > 0 ? 1.0 : 0.0;
        return new WindowsMediaOcrResult(words, overallConfidence);
    }

    private static SoftwareBitmap CreateSoftwareBitmap(IRawImage image)
    {
        var packed = ToTightBgra(image);

        using var writer = new DataWriter();
        writer.WriteBytes(packed);
        var buffer = writer.DetachBuffer();

        return SoftwareBitmap.CreateCopyFromBuffer(
            buffer,
            BitmapPixelFormat.Bgra8,
            image.Width,
            image.Height,
            BitmapAlphaMode.Ignore);
    }

    private static byte[] ToTightBgra(IRawImage image)
    {
        var pixels = image.ToByteArray();
        var width = image.Width;
        var height = image.Height;
        var srcStride = image.Stride;
        var dstStride = width * 4;
        var expectedLength = dstStride * height;

        if (srcStride == dstStride && pixels.Length == expectedLength)
            return pixels;

        var packed = new byte[expectedLength];
        var copyWidth = Math.Min(srcStride, dstStride);
        for (var y = 0; y < height; y++)
            System.Buffer.BlockCopy(pixels, y * srcStride, packed, y * dstStride, copyWidth);
        return packed;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }
}
