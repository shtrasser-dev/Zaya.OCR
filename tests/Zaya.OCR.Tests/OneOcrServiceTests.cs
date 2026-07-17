using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Xunit;
using Zaya.OCR.Impl.OneOcr;
using Zaya.OCR.Impl.OneOcr.Services;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Tests;

public sealed class OneOcrServiceTests : IAsyncLifetime
{
    private OneOcrService? _service;
    private IOCRSession? _session;
    private bool _modelAvailable;

    public async ValueTask InitializeAsync()
    {
        _service = new OneOcrService();

        try
        {
            await _service.InitializeAsync(null);
            _session = await _service.CreateSessionAsync(cancellationToken: TestContext.Current.CancellationToken);
            _modelAvailable = _service.IsAvailable;
        }
        catch (LocalizedException)
        {
            _modelAvailable = false;
        }
        catch (InvalidOperationException)
        {
            _modelAvailable = false;
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
        var name = _service!.DisplayName.GetValue(System.Globalization.CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(name));
    }

    [Fact]
    public void Settings_ReturnsExpectedDescriptors()
    {
        var settings = _service!.Settings;
        Assert.NotEmpty(settings);
        Assert.Contains(settings, s => s.Key == "source");
        Assert.Contains(settings, s => s.Key == "directoryPath");
        Assert.Contains(settings, s => s.Key == "downloadUrl");
        Assert.Contains(settings, s => s.Key == "cacheDirectory");
    }

    [Fact]
    public void Settings_Predicates_AreCorrect()
    {
        var settings = _service!.Settings.ToDictionary(s => s.Key);

        // source
        Assert.True(settings["source"].IsVisible(new Dictionary<string, object?>()));
        Assert.True(settings["source"].IsRequired(new Dictionary<string, object?>()));
        Assert.True(settings["source"].IsRequired(new Dictionary<string, object?> { ["source"] = "directory" }));

        // directoryPath — visible+required only when source == "directory"
        Assert.False(settings["directoryPath"].IsVisible(new Dictionary<string, object?>()));
        Assert.False(settings["directoryPath"].IsVisible(new Dictionary<string, object?> { ["source"] = "snippingtool" }));
        Assert.False(settings["directoryPath"].IsRequired(new Dictionary<string, object?>()));
        Assert.True(settings["directoryPath"].IsVisible(new Dictionary<string, object?> { ["source"] = "directory" }));
        Assert.True(settings["directoryPath"].IsRequired(new Dictionary<string, object?> { ["source"] = "directory" }));

        // downloadUrl — visible+required only when source == "url"
        Assert.False(settings["downloadUrl"].IsVisible(new Dictionary<string, object?>()));
        Assert.False(settings["downloadUrl"].IsVisible(new Dictionary<string, object?> { ["source"] = "directory" }));
        Assert.False(settings["downloadUrl"].IsRequired(new Dictionary<string, object?>()));
        Assert.True(settings["downloadUrl"].IsVisible(new Dictionary<string, object?> { ["source"] = "url" }));
        Assert.True(settings["downloadUrl"].IsRequired(new Dictionary<string, object?> { ["source"] = "url" }));

        // cacheDirectory — visible for snippingtool or url, hidden for directory
        Assert.True(settings["cacheDirectory"].IsVisible(new Dictionary<string, object?>()));
        Assert.True(settings["cacheDirectory"].IsVisible(new Dictionary<string, object?> { ["source"] = "snippingtool" }));
        Assert.True(settings["cacheDirectory"].IsVisible(new Dictionary<string, object?> { ["source"] = "url" }));
        Assert.False(settings["cacheDirectory"].IsVisible(new Dictionary<string, object?> { ["source"] = "directory" }));
        // cacheDirectory is not required
        Assert.False(settings["cacheDirectory"].IsRequired(new Dictionary<string, object?>()));
    }

    [Fact]
    public void EngineId_ReturnsOneocr()
    {
        Assert.Equal("oneocr", _service!.EngineId);
    }

    [Fact]
    public void PreferredPixelFormat_IsBgra32()
    {
        Assert.Equal(Zaya.Primitives.PixelFormat.Bgra32, _service!.PreferredPixelFormat);
    }

    [Fact]
    public async Task CreateSession_WithoutInitialize_Throws()
    {
        var service = new OneOcrService();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateSessionAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        var service = new OneOcrService();

        try
        {
            await service.InitializeAsync(null, TestContext.Current.CancellationToken);
            // Second call should not throw — idempotent
            await service.InitializeAsync(null, TestContext.Current.CancellationToken);
            Assert.True(service.IsAvailable);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task CreateSession_Succeeds()
    {
        if (!_modelAvailable) return;

        using var session = await _service!.CreateSessionAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(session);
    }

    [Fact]
    public async Task RecognizeAsync_SimpleImage_ReturnsExpectedText()
    {
        if (!_modelAvailable) return;

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
    public async Task RecognizeAsync_MultipleLines_ReturnsAllLines()
    {
        if (!_modelAvailable) return;

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
        if (!_modelAvailable) return;

        var image = CreateEmptyImage(200, 100);

        var result = await _session!.RecognizeAsync(image, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result.Words);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public async Task CancellationToken_CancelsOperation()
    {
        if (!_modelAvailable) return;

        var image = CreateTestImage("Hello World", 400, 100, 48);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _session!.RecognizeAsync(image, cts.Token));
    }

    [Fact]
    public async Task RecognizeAsync_EachWordHasBounds()
    {
        if (!_modelAvailable) return;

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
    public async Task InitializeAsync_WithDirectorySource_MissingPath_Throws()
    {
        var service = new OneOcrService();
        try
        {
            var settings = new Dictionary<string, object?>
            {
                ["source"] = "directory",
                ["directoryPath"] = @"C:\nonexistent\path"
            };
            await Assert.ThrowsAsync<OneOcrDllNotFoundException>(() =>
                service.InitializeAsync(settings, TestContext.Current.CancellationToken));
        }
        finally
        {
            service.Dispose();
        }
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
        var bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

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
