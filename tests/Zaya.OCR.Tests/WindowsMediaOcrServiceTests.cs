using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using Zaya.OCR.Impl.WindowsMediaOcr;
using Zaya.OCR.Impl.WindowsMediaOcr.Exceptions;
using Zaya.OCR.Impl.WindowsMediaOcr.Extensions;
using Zaya.OCR.Impl.WindowsMediaOcr.Models;
using Zaya.OCR.Services;
using Zaya.Primitives;
using DrawingPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace Zaya.OCR.Tests;

public sealed class WindowsMediaOcrServiceTests : IAsyncLifetime
{
    private WindowsMediaOcrService? _service;
    private IOCRSession? _session;
    private bool _engineAvailable;

    public async ValueTask InitializeAsync()
    {
        _service = new WindowsMediaOcrService();
        if (!_service.IsAvailable)
        {
            _engineAvailable = false;
            return;
        }

        try
        {
            _session = await _service.CreateSessionAsync(
                new Dictionary<string, object> { ["language"] = "auto" },
                TestContext.Current.CancellationToken);
            _engineAvailable = true;
        }
        catch (LocalizedException)
        {
            _engineAvailable = false;
        }
    }

    public ValueTask DisposeAsync()
    {
        _session?.Dispose();
        _service?.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public void DisplayName_ReturnsNonEmpty()
    {
        var name = _service!.DisplayName.GetValue(CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(name));
    }

    [Fact]
    public void Description_ReturnsNonEmpty()
    {
        var description = _service!.Description.GetValue(CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(description));
    }

    [Fact]
    public void EngineId_ReturnsWindowsMediaOcr()
    {
        Assert.Equal("windows-media-ocr", _service!.EngineId);
    }

    [Fact]
    public void PreferredPixelFormat_IsBgra32()
    {
        Assert.Equal(Zaya.Primitives.PixelFormat.Bgra32, _service!.PreferredPixelFormat);
    }

    [Fact]
    public void Settings_ReturnsLanguageDescriptor()
    {
        var settings = _service!.Settings;
        Assert.Single(settings);
        Assert.Contains(settings, s => s.Key == "language");

        var language = Assert.IsType<EnumSettingDescriptor>(settings.Single(s => s.Key == "language"));
        Assert.Equal("auto", language.DefaultValue);
        Assert.Contains(language.Options, o => o.Value == "auto");
        Assert.True(language.Options.Count >= 1);
    }

    [Fact]
    public void Settings_Language_IsRequiredAndVisible()
    {
        var language = _service!.Settings.Single(s => s.Key == "language");
        var empty = new Dictionary<string, object?>();

        Assert.True(language.IsVisible(empty));
        Assert.True(language.IsRequired(empty));
    }

    [Fact]
    public void Config_ToDictionary_MapsLanguage()
    {
        var config = new WindowsMediaOcrConfig { Language = "en" };
        var dict = config.ToDictionary();

        Assert.Equal("en", dict["language"]);
    }

    [Fact]
    public async Task CreateSession_WithDefaults_Succeeds()
    {
        if (!_engineAvailable) return;

        using var session = await _service!.CreateSessionAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(session);
    }

    [Fact]
    public async Task CreateSession_WithAutoLanguage_Succeeds()
    {
        if (!_engineAvailable) return;

        using var session = await _service!.CreateSessionAsync(
            new Dictionary<string, object> { ["language"] = "auto" },
            TestContext.Current.CancellationToken);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task CreateSession_WithUnsupportedLanguage_Throws()
    {
        using var service = new WindowsMediaOcrService();
        if (!service.IsAvailable) return;

        var settings = new Dictionary<string, object>
        {
            ["language"] = "xx-ZZ",
        };

        await Assert.ThrowsAnyAsync<LocalizedException>(() =>
            service.CreateSessionAsync(settings, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CreateSession_WithConfigDictionary_Succeeds()
    {
        if (!_engineAvailable) return;

        var config = new WindowsMediaOcrConfig { Language = "auto" };
        var settings = config.ToDictionary()
            .Where(kv => kv.Value is not null)
            .ToDictionary(kv => kv.Key, kv => kv.Value!);

        using var session = await _service!.CreateSessionAsync(settings, TestContext.Current.CancellationToken);
        Assert.NotNull(session);
    }

    [Fact]
    public async Task RecognizeAsync_SimpleImage_ReturnsExpectedText()
    {
        if (!_engineAvailable) return;

        var image = CreateTestImage("Hello World", 400, 100, 48);
        var result = await _session!.RecognizeAsync(image, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Words);
        Assert.True(result.Confidence > 0);

        var fullText = string.Join(" ", result.Words.Select(w => w.Text));
        Assert.Contains("Hello", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("World", fullText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecognizeAsync_BitmapExtension_ReturnsExpectedText()
    {
        if (!_engineAvailable) return;

        using var bitmap = new Bitmap(400, 100);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
            using var font = new Font("Segoe UI", 48, FontStyle.Regular, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(Color.Black);
            graphics.DrawString("Hello World", font, brush, new PointF(10, 10));
        }

        var result = await _session!.RecognizeAsync(bitmap, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Words);

        var fullText = string.Join(" ", result.Words.Select(w => w.Text));
        Assert.Contains("Hello", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("World", fullText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecognizeAsync_MultipleLines_ReturnsAllLines()
    {
        if (!_engineAvailable) return;

        var image = CreateTestImage("Line One\nLine Two\nLine Three", 400, 200, 24);
        var result = await _session!.RecognizeAsync(image, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Words);

        var fullText = string.Join(" ", result.Words.Select(w => w.Text));
        Assert.Contains("Line", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Words.Count >= 3);
    }

    [Fact]
    public async Task RecognizeAsync_EmptyImage_ReturnsEmptyResult()
    {
        if (!_engineAvailable) return;

        var image = CreateEmptyImage(200, 100);
        var result = await _session!.RecognizeAsync(image, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result.Words);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public async Task RecognizeAsync_UnsupportedPixelFormat_Throws()
    {
        if (!_engineAvailable) return;

        var pixels = new byte[200 * 100];
        using var image = new TestRawImage(pixels, 200, 100, 200, Zaya.Primitives.PixelFormat.Gray8);

        await Assert.ThrowsAsync<WindowsMediaOcrUnsupportedPixelFormatException>(() =>
            _session!.RecognizeAsync(image, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CancellationToken_CancelsOperation()
    {
        if (!_engineAvailable) return;

        var image = CreateTestImage("Hello World", 400, 100, 48);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _session!.RecognizeAsync(image, cts.Token));
    }

    [Fact]
    public async Task RecognizeAsync_EachWordHasBounds()
    {
        if (!_engineAvailable) return;

        var image = CreateTestImage("Hello World", 400, 100, 48);
        var result = await _session!.RecognizeAsync(image, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Words);

        foreach (var word in result.Words)
        {
            Assert.False(string.IsNullOrWhiteSpace(word.Text));
            Assert.True(word.Confidence > 0);
            Assert.True(word.Bounds.Width > 0);
            Assert.True(word.Bounds.Height > 0);
        }
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var service = new WindowsMediaOcrService();
        service.Dispose();
        service.Dispose();
    }

    [Fact]
    public async Task CreateSession_AfterDispose_Throws()
    {
        var service = new WindowsMediaOcrService();
        service.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            service.CreateSessionAsync(TestContext.Current.CancellationToken));
    }

    private static IRawImage CreateTestImage(string text, int width, int height, int fontSize)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        using var font = new Font("Segoe UI", fontSize, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.Black);

        var lines = text.Split('\n');
        var currentY = 10f;
        foreach (var line in lines)
        {
            graphics.DrawString(line, font, brush, new PointF(10, currentY));
            currentY += graphics.MeasureString(line, font).Height * 1.2f;
        }

        return BitmapToRawImage(bitmap);
    }

    private static IRawImage CreateEmptyImage(int width, int height)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        return BitmapToRawImage(bitmap);
    }

    private static IRawImage BitmapToRawImage(Bitmap bitmap)
    {
        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, DrawingPixelFormat.Format32bppArgb);

        var stride = bmpData.Stride;
        var pixels = new byte[stride * bitmap.Height];
        System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixels, 0, pixels.Length);
        bitmap.UnlockBits(bmpData);

        return new TestRawImage(pixels, bitmap.Width, bitmap.Height, stride, Zaya.Primitives.PixelFormat.Bgra32);
    }

    private sealed class TestRawImage : IRawImage
    {
        private readonly byte[] _pixels;
        public int Width { get; }
        public int Height { get; }
        public int Stride { get; }
        public Zaya.Primitives.PixelFormat Format { get; }

        public TestRawImage(byte[] pixels, int width, int height, int stride, Zaya.Primitives.PixelFormat format)
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
