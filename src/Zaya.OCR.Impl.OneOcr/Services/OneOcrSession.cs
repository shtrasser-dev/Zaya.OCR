using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Zaya.OCR.Impl.OneOcr.Models;
using Zaya.OCR.Models;
using Zaya.OCR.Services;

namespace Zaya.OCR.Impl.OneOcr.Services;

/// <summary>
/// OneOCR session that holds recognition options and performs OCR
/// using the native oneocr.dll via P/Invoke.
/// </summary>
public sealed class OneOcrSession : IOCRSession
{
    private bool _disposed;

    internal OneOcrSession(OcrOptions options)
    {
        Options = options;
    }

    /// <inheritdoc />
    public OcrOptions Options { get; }

    /// <inheritdoc />
    public Task<IOCRResult> RecognizeAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] pixelData;
        int width, height, stride;

        if (Options.Width.HasValue && Options.Height.HasValue)
        {
            width = Options.Width.Value;
            height = Options.Height.Value;
            stride = Options.Stride ?? width * 4;
            pixelData = data;
        }
        else
        {
            (pixelData, width, height, stride) = DecodeImageToBgra(data);
        }

        var nativeWords = OneOcrNative.Recognize(pixelData, width, height, stride);

        var words = nativeWords
            .Select(w => (IOCRWord)new OneOcrWord(w.Text, w.Bounds, w.Confidence))
            .ToList();

        var overallConfidence = words.Count > 0
            ? words.Average(w => w.Confidence)
            : 0.0;

        return Task.FromResult<IOCRResult>(new OneOcrResult(words, overallConfidence));
    }

    private static (byte[] pixels, int width, int height, int stride) DecodeImageToBgra(byte[] encodedData)
    {
        using var ms = new MemoryStream(encodedData);
        using var bitmap = new Bitmap(ms);

        var width = bitmap.Width;
        var height = bitmap.Height;
        var rect = new Rectangle(0, 0, width, height);
        var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            var stride = bmpData.Stride;
            var pixelCount = stride * height;
            var pixels = new byte[pixelCount];
            Marshal.Copy(bmpData.Scan0, pixels, 0, pixelCount);
            return (pixels, width, height, stride);
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
