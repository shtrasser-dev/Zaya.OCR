using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Zaya.OCR.Models;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.OneOcr;

/// <summary>
/// Extension methods for <see cref="IOCRSession"/> to accept <see cref="Bitmap"/> input.
/// </summary>
public static class OcrSessionExtensions
{
    /// <summary>
    /// Recognizes text from a <see cref="Bitmap"/> by converting it to raw BGRA pixels
    /// and delegating to <see cref="IOCRSession.RecognizeAsync(IRawImage, CancellationToken)"/>.
    /// </summary>
    /// <param name="session">The OCR session.</param>
    /// <param name="bitmap">The bitmap to recognize text from.</param>
    /// <param name="cancellationToken">Token to cancel the recognition operation.</param>
    /// <returns>The OCR result containing recognized words and confidence.</returns>
    public static async Task<IOCRResult> RecognizeAsync(
        this IOCRSession session,
        Bitmap bitmap,
        CancellationToken cancellationToken = default)
    {
        var rawImage = BitmapToRawImage(bitmap);
        using (rawImage)
            return await session.RecognizeAsync(rawImage, cancellationToken);
    }

    private static IRawImage BitmapToRawImage(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        try
        {
            var stride = bmpData.Stride;
            var pixelCount = stride * bitmap.Height;
            var pixels = new byte[pixelCount];
            Marshal.Copy(bmpData.Scan0, pixels, 0, pixelCount);

            return new RawImageWrapper(pixels, bitmap.Width, bitmap.Height, stride, Zaya.Primitives.PixelFormat.Bgra32);
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }

    private sealed class RawImageWrapper : IRawImage
    {
        private readonly byte[] _pixels;

        public int Width { get; }
        public int Height { get; }
        public int Stride { get; }
        public Zaya.Primitives.PixelFormat Format { get; }

        public RawImageWrapper(byte[] pixels, int width, int height, int stride, Zaya.Primitives.PixelFormat format)
        {
            _pixels = pixels;
            Width = width;
            Height = height;
            Stride = stride;
            Format = format;
        }

        public ReadOnlySpan<byte> GetPixelData() => _pixels;
        public byte[] ToByteArray() => _pixels;
        public void Dispose() { }
    }
}
