using System.Drawing;
using System.Drawing.Imaging;
using Zaya.OCR.Impl.OneOcr.Services;
using Zaya.OCR.Models;
using Zaya.OCR.Services;

namespace Zaya.OCR.Tests;

public sealed class OneOcrServiceTests : IAsyncLifetime
{
    private OneOcrService? _service;
    private IOCRSession? _session;
    private bool _modelAvailable;

    public async ValueTask InitializeAsync()
    {
        _service = new OneOcrService();
        _session = await _service.CreateSessionAsync();

        try
        {
            var result = await _session.RecognizeAsync(CreateEmptyImage(100, 50));
            _modelAvailable = result is not null;
        }
        catch (UnauthorizedAccessException)
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
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task CreateSession_SetsOptions()
    {
        var lang = "ru-RU";
        var options = new OcrOptions { Language = lang };

        using var session = await _service!.CreateSessionAsync(options);

        Assert.Same(options, session.Options);
        Assert.Equal(lang, session.Options.Language);
    }

    [Fact]
    public async Task CreateSession_NullOptions_UsesDefaults()
    {
        using var session = await _service!.CreateSessionAsync();

        Assert.NotNull(session.Options);
        Assert.Null(session.Options.Language);
        Assert.True(session.Options.EnableWordLevelConfidence);
    }

    [Fact]
    public async Task RecognizeAsync_SimpleImage_ReturnsExpectedText()
    {
        if (!_modelAvailable)
            return;

        var imageBytes = CreateTestImage("Hello World", 400, 100, 48);

        var result = await _session!.RecognizeAsync(imageBytes);

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
        if (!_modelAvailable)
            return;

        var imageBytes = CreateTestImage("Line One\nLine Two\nLine Three", 400, 200, 24);

        var result = await _session!.RecognizeAsync(imageBytes);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Words);

        var fullText = string.Join(" ", result.Words.Select(w => w.Text));
        Assert.Contains("Line", fullText, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.Words.Count >= 3);
    }

    [Fact]
    public async Task RecognizeAsync_EmptyImage_ReturnsEmptyResult()
    {
        if (!_modelAvailable)
            return;

        var imageBytes = CreateEmptyImage(200, 100);

        var result = await _session!.RecognizeAsync(imageBytes);

        Assert.NotNull(result);
        Assert.Empty(result.Words);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public async Task CancellationToken_CancelsOperation()
    {
        var imageBytes = CreateTestImage("Hello World", 400, 100, 48);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _session!.RecognizeAsync(imageBytes, cts.Token));
    }

    [Fact]
    public async Task RecognizeAsync_EachWordHasBounds()
    {
        if (!_modelAvailable)
            return;

        var imageBytes = CreateTestImage("Hello World", 400, 100, 48);

        var result = await _session!.RecognizeAsync(imageBytes);

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

    private static byte[] CreateTestImage(string text, int width, int height, int fontSize)
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

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static byte[] CreateEmptyImage(int width, int height)
    {
        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
