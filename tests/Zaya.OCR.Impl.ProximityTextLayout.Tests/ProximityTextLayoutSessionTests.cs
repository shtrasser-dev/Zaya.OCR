using System.Drawing;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Impl.ProximityTextLayout.Services;
using Zaya.OCR.Models;
using Zaya.OCR.Services;

namespace Zaya.OCR.Impl.ProximityTextLayout.Tests;

public sealed class ProximityTextLayoutSessionTests
{
    private static ITextLayoutSession CreateSession(
        double wordGap = 0.5,
        double baselineDrift = 0.5,
        double lineSpacing = 1.5,
        double leftEdgeAlign = 1.0,
        double firstLineIndent = 3.0,
        bool centerAlign = false,
        double fontSizeTolerance = 0.5,
        bool enableStabilization = false)
    {
        var options = new ProximityTextLayoutOptions(
            wordGap, baselineDrift, lineSpacing, leftEdgeAlign, firstLineIndent, centerAlign, fontSizeTolerance,
            EnableStabilization: enableStabilization);
        return new ProximityTextLayoutSession(options);
    }

    private static IOCRResult CreateResult(params IOCRWord[] words) => new StubResult(words);

    private static IOCRWord MakeWord(string text, int x, int y, int w, int h, double confidence = 1.0)
        => new StubWord(text, new Rectangle(x, y, w, h), confidence);

    [Fact]
    public async Task ProcessAsync_EmptyWords_ReturnsEmptyBlocks()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(), TestContext.Current.CancellationToken);

        Assert.Empty(result.Paragraphs);
        Assert.Equal("", result.FullText);
    }

    [Fact]
    public async Task ProcessAsync_SingleWord_ReturnsOneBlockOneLine()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Hello", 10, 10, 50, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Single(result.Paragraphs[0].Lines);
        Assert.Equal("Hello", result.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_TwoWordsSameLine_MergesIntoOneLine()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Hello", 10, 10, 45, 20),
            MakeWord("World", 59, 10, 45, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Single(result.Paragraphs[0].Lines);
        Assert.Equal("Hello World", result.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_TwoWordsWideGap_SplitsIntoTwoLines()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Hello", 10, 10, 30, 20),
            MakeWord("World", 90, 10, 30, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Paragraphs.Count);
        Assert.Equal("Hello", result.Paragraphs[0].Text);
        Assert.Equal("World", result.Paragraphs[1].Text);
    }

    [Fact]
    public async Task ProcessAsync_TwoWordsVerticalDrift_SplitsIntoLines()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Hello", 10, 10, 50, 20),
            MakeWord("World", 14, 28, 50, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal(2, result.Paragraphs[0].Lines.Count);
    }

    [Fact]
    public async Task ProcessAsync_TwoLinesSameParagraph_MergesIntoBlock()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Line", 10, 10, 40, 20),
            MakeWord("One", 54, 10, 40, 20),
            MakeWord("Line", 10, 40, 40, 20),
            MakeWord("Two", 54, 40, 40, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal(2, result.Paragraphs[0].Lines.Count);
        Assert.Equal("Line One\nLine Two", result.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_TwoLinesVerticalGap_SplitsIntoBlocks()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Line", 10, 10, 40, 20),
            MakeWord("One", 54, 10, 40, 20),
            MakeWord("Line", 10, 75, 40, 20),
            MakeWord("Two", 54, 75, 40, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Paragraphs.Count);
    }

    [Fact]
    public async Task ProcessAsync_LeftEdgeMismatch_SplitsIntoBlocks()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Line", 10, 10, 40, 20),
            MakeWord("One", 54, 10, 40, 20),
            MakeWord("Line", 100, 40, 40, 20),
            MakeWord("Two", 144, 40, 40, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Paragraphs.Count);
    }

    [Fact]
    public async Task ProcessAsync_IndentedFirstLine_MergesIntoBlock()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("First", 50, 10, 40, 20),
            MakeWord("Second", 10, 40, 45, 20),
            MakeWord("Third", 10, 70, 40, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal(3, result.Paragraphs[0].Lines.Count);
    }

    [Fact]
    public async Task ProcessAsync_DeeplyIndentedFirstLine_SplitsIntoBlocks()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("First", 120, 10, 40, 20),
            MakeWord("Second", 10, 40, 45, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Paragraphs.Count);
    }

    [Fact]
    public async Task ProcessAsync_CenterAlignment_MergesCenteredLines()
    {
        using var session = CreateSession(centerAlign: true);
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Title", 180, 10, 40, 20),
            MakeWord("Subtitle", 90, 40, 220, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
    }

    [Fact]
    public async Task ProcessAsync_CenterAlignmentDisabled_SplitsCenteredLines()
    {
        using var session = CreateSession(centerAlign: false);
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Title", 180, 10, 40, 20),
            MakeWord("Subtitle", 90, 40, 220, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Paragraphs.Count);
    }

    [Fact]
    public async Task ProcessAsync_MultipleParagraphs_FullText_JoinsWithEmptyLines()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("First", 10, 10, 45, 20),
            MakeWord("Second", 10, 75, 50, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Paragraphs.Count);
        Assert.Equal("First\n\nSecond", result.FullText);
    }

    [Fact]
    public async Task ProcessAsync_CancellationToken_CancelsOperation()
    {
        using var session = CreateSession();
        var cts = new CancellationTokenSource();
        cts.Cancel();

#pragma warning disable xUnit1051
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            session.ProcessAsync(CreateResult(MakeWord("Hello", 10, 10, 50, 20)), cts.Token));
#pragma warning restore xUnit1051
    }

    [Fact]
    public async Task ProcessAsync_TextLineBounds_EncompassAllWords()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Hello", 10, 10, 45, 20),
            MakeWord("World", 59, 10, 45, 20)
        ), TestContext.Current.CancellationToken);

        var line = result.Paragraphs[0].Lines[0];
        Assert.Equal(10, line.Bounds.Left);
        Assert.Equal(10, line.Bounds.Top);
        Assert.Equal(104, line.Bounds.Right);
        Assert.Equal(30, line.Bounds.Bottom);
    }

    [Fact]
    public async Task ProcessAsync_MixedWordHeights_StillMergesByCenters()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("WORD", 10, 8, 60, 24),
            MakeWord("text", 78, 13, 30, 14)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Single(result.Paragraphs[0].Lines);
    }

    [Fact]
    public async Task ProcessAsync_WordsInReadingOrder_PreserveOrder()
    {
        using var session = CreateSession();
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("2",        10, 10, 14, 22),
            MakeWord("ce3oh",    28, 10, 38, 24),
            MakeWord("7",        75, 10, 12, 20),
            MakeWord("cepия",    95, 10, 35, 22),
            MakeWord("Koнтент",  10, 45, 55, 26),
            MakeWord("для",      70, 46, 25, 20),
            MakeWord("взpocлыx", 100, 44, 62, 24)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal(2, result.Paragraphs[0].Lines.Count);

        var line1Text = result.Paragraphs[0].Lines[0].Text;
        var line2Text = result.Paragraphs[0].Lines[1].Text;

        Assert.Contains("2", line1Text);
        Assert.Contains("ce3oh", line1Text);
        Assert.Contains("7", line1Text);
        Assert.Contains("cepия", line1Text);

        Assert.Contains("Koнтент", line2Text);
        Assert.Contains("для", line2Text);
        Assert.Contains("взpocлыx", line2Text);

        var line1Words = result.Paragraphs[0].Lines[0].Words;
        Assert.Equal("2", line1Words[0].Text);
        Assert.Equal("ce3oh", line1Words[1].Text);
        Assert.Equal("7", line1Words[2].Text);
        Assert.Equal("cepия", line1Words[3].Text);
    }

    [Fact]
    public async Task ProcessAsync_ShorterFlicker_KeepsPreviousParagraph()
    {
        using var session = CreateSession(enableStabilization: true);
        const string full =
            "I first ended up in this place two months ago so I had a bit of time";
        const string shorter =
            "I first ended up in this place two months ago so had a bit of time";

        var first = await session.ProcessAsync(
            CreateResult(MakeWord(full, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        var second = await session.ProcessAsync(
            CreateResult(MakeWord(shorter, 12, 11, 395, 20)), TestContext.Current.CancellationToken);

        Assert.Equal(first.Paragraphs[0].Text, second.Paragraphs[0].Text);
        Assert.Equal(first.Paragraphs[0].Lines[0].Bounds, second.Paragraphs[0].Lines[0].Bounds);
    }

    [Fact]
    public async Task ProcessAsync_LongerUpgrade_TakesNewParagraph()
    {
        using var session = CreateSession(enableStabilization: true);
        const string shorter =
            "I first ended up in this place two months ago so had a bit of time";
        const string full =
            "I first ended up in this place two months ago so I had a bit of time";

        var first = await session.ProcessAsync(
            CreateResult(MakeWord(shorter, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        var second = await session.ProcessAsync(
            CreateResult(MakeWord(full, 12, 11, 410, 21)), TestContext.Current.CancellationToken);

        Assert.Equal(full, second.Paragraphs[0].Lines[0].Text);
        Assert.Equal(new Rectangle(12, 11, 410, 21), second.Paragraphs[0].Lines[0].Bounds);
        Assert.NotEqual(first.Paragraphs[0].Lines[0].Bounds, second.Paragraphs[0].Lines[0].Bounds);
    }

    private sealed class StubWord : IOCRWord
    {
        public string Text { get; }
        public Rectangle Bounds { get; }
        public double Confidence { get; }

        public StubWord(string text, Rectangle bounds, double confidence)
        {
            Text = text;
            Bounds = bounds;
            Confidence = confidence;
        }
    }

    private sealed class StubResult : IOCRResult
    {
        public IReadOnlyList<IOCRWord> Words { get; }
        public double Confidence { get; }

        public StubResult(params IOCRWord[] words)
        {
            Words = words;
            Confidence = words.Length > 0 ? words.Average(w => w.Confidence) : 0;
        }
    }
}
