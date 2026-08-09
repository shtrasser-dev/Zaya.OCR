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
        bool enableStabilization = false,
        bool holdNewBlocks = false,
        double paragraphMergeHysteresis = 1.2,
        double sameLineWordGapHysteresis = 6.0,
        int ghostMaxFrames = 3,
        LayoutTextFilter? wordFilter = null,
        LayoutTextFilter? lineFilter = null,
        LayoutTextFilter? paragraphFilter = null)
    {
        var options = new ProximityTextLayoutOptions(
            wordGap, baselineDrift, lineSpacing, leftEdgeAlign, firstLineIndent, centerAlign, fontSizeTolerance,
            EnableStabilization: enableStabilization,
            ParagraphMergeHysteresis: paragraphMergeHysteresis,
            HoldNewBlocks: holdNewBlocks,
            GhostMaxFrames: ghostMaxFrames,
            SameLineWordGapHysteresis: sameLineWordGapHysteresis);
        return new ProximityTextLayoutSession(options, wordFilter, lineFilter, paragraphFilter);
    }

    private static IOCRResult CreateResult(params IOCRWord[] words) => new StubResult(words);

    private static IOCRWord MakeWord(string text, int x, int y, int w, int h, double confidence = 1.0)
        => new StubWord(text, BoundingBox.FromAxisAligned(x, y, w, h), confidence);

    private static IOCRWord MakeOrientedWord(
        string text,
        float x1, float y1,
        float x2, float y2,
        float x3, float y3,
        float x4, float y4,
        double confidence = 1.0)
        => new StubWord(
            text,
            new BoundingBox(
                new System.Numerics.Vector2(x1, y1),
                new System.Numerics.Vector2(x2, y2),
                new System.Numerics.Vector2(x3, y3),
                new System.Numerics.Vector2(x4, y4)),
            confidence);

    [Fact]
    public async Task ProcessAsync_TiltedWords_MergesIntoOneLine()
    {
        // Two words on a ~26.5° diagonal baseline (rise/run = 0.5).
        using var session = CreateSession(wordGap: 1.0, baselineDrift: 0.6);
        var result = await session.ProcessAsync(CreateResult(
            MakeOrientedWord("Hello", 10, 40, 60, 65, 56, 81, 6, 56),
            MakeOrientedWord("World", 70, 70, 120, 95, 116, 111, 66, 86)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Single(result.Paragraphs[0].Lines);
        Assert.Equal("Hello World", result.Paragraphs[0].Lines[0].Text);
        Assert.InRange(Math.Abs(result.Paragraphs[0].Lines[0].Bounds.AngleDegrees), 20, 35);
    }

    [Fact]
    public async Task ProcessAsync_TiltedLines_MergesIntoParagraph()
    {
        // Two left-aligned lines on a ~26.5° tilt (second shifted along the paragraph normal).
        using var session = CreateSession(lineSpacing: 2.0, leftEdgeAlign: 1.0);
        var result = await session.ProcessAsync(CreateResult(
            MakeOrientedWord("Hello", 10, 40, 80, 75, 76, 91, 6, 56),
            MakeOrientedWord("World", -1, 62, 69, 97, 65, 113, -5, 78)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal(2, result.Paragraphs[0].Lines.Count);
        Assert.Equal("Hello\nWorld", result.Paragraphs[0].Text);
    }

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
    public async Task ProcessAsync_WordFilterSkip_MultipleWords_RemovesBeforeLineMerge()
    {
        var filter = LayoutTextFilter.FromRules([
            ("spam", IsRegex: false, Strip: false),
            ("noise", IsRegex: false, Strip: false),
        ]);
        using var session = CreateSession(wordFilter: filter);
        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Hello", 10, 10, 45, 20),
            MakeWord("spam", 20, 50, 40, 20),
            MakeWord("World", 59, 10, 45, 20),
            MakeWord("NOISE", 80, 50, 40, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Single(result.Paragraphs[0].Lines);
        Assert.Equal("Hello World", result.Paragraphs[0].Text);
        Assert.Equal(2, result.Paragraphs[0].Lines[0].Words.Count);
        Assert.DoesNotContain(result.Paragraphs[0].Lines[0].Words, w =>
            w.Text.Equals("spam", StringComparison.OrdinalIgnoreCase)
            || w.Text.Equals("noise", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcessAsync_OneCharacterChange_KeepsEqualLengthPrevious()
    {
        using var session = CreateSession(enableStabilization: true);
        const string a = "POPULATION III STAR.";
        const string b = "POPULATION II STAR."; // one char shorter — keep longer previous
        const string c = "POPULATIOX III STAR."; // same length, one char flipped — keep previous

        Assert.True(a.Length > b.Length);
        Assert.Equal(a.Length, c.Length);

        await session.ProcessAsync(
            CreateResult(MakeWord(a, 10, 10, 200, 20)), TestContext.Current.CancellationToken);

        var shorter = await session.ProcessAsync(
            CreateResult(MakeWord(b, 10, 10, 200, 20)), TestContext.Current.CancellationToken);
        Assert.Equal(a, shorter.Paragraphs[0].Text);

        var flipped = await session.ProcessAsync(
            CreateResult(MakeWord(c, 10, 10, 200, 20)), TestContext.Current.CancellationToken);
        Assert.Equal(a, flipped.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_CaseAndPunctuationVariant_KeepsFirstEmitted()
    {
        using var session = CreateSession(enableStabilization: true);

        var first = await session.ProcessAsync(
            CreateResult(MakeWord("LV.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Equal("LV.80", first.Paragraphs[0].Text);

        for (var i = 0; i < 3; i++)
        {
            var frame = await session.ProcessAsync(
                CreateResult(MakeWord("Lv.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);
            Assert.Single(frame.Paragraphs);
            Assert.Equal("LV.80", frame.Paragraphs[0].Text);
        }
    }

    [Fact]
    public async Task ProcessAsync_ShorterFlicker_KeepsAlreadyEmittedParagraph()
    {
        using var session = CreateSession(enableStabilization: true);
        const string full =
            "I first ended up in this place two months ago so I had a bit of time";
        const string shorter =
            "I first ended up in this place two months ago so had a bit of time";

        var first = await session.ProcessAsync(
            CreateResult(MakeWord(full, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Single(first.Paragraphs);

        var second = await session.ProcessAsync(
            CreateResult(MakeWord(shorter, 12, 11, 395, 20)), TestContext.Current.CancellationToken);
        Assert.Single(second.Paragraphs);
        Assert.Equal(full, second.Paragraphs[0].Text);
        Assert.Equal(first.Paragraphs[0].Lines[0].Bounds.P5.X, second.Paragraphs[0].Lines[0].Bounds.P5.X, precision: 1);
        Assert.Equal(first.Paragraphs[0].Lines[0].Bounds.P5.Y, second.Paragraphs[0].Lines[0].Bounds.P5.Y, precision: 1);
    }

    [Fact]
    public async Task ProcessAsync_LongerUpgrade_KeepsOldUntilStableThenUpgrades()
    {
        using var session = CreateSession(enableStabilization: true);
        const string shorter =
            "I first ended up in this place two months ago so had a bit of time";
        const string full =
            "I first ended up in this place two months ago so I had a bit of time";

        var first = await session.ProcessAsync(
            CreateResult(MakeWord(shorter, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Single(first.Paragraphs);
        Assert.Equal(shorter, first.Paragraphs[0].Text);

        var growing = await session.ProcessAsync(
            CreateResult(MakeWord(full, 12, 11, 410, 21)), TestContext.Current.CancellationToken);
        Assert.Single(growing.Paragraphs);
        // Fuzzy match + longer wins: upgrade immediately when readings are similar.
        Assert.Equal(full, growing.Paragraphs[0].Text);

        var stable = await session.ProcessAsync(
            CreateResult(MakeWord(full, 12, 11, 410, 21)), TestContext.Current.CancellationToken);
        Assert.Single(stable.Paragraphs);
        Assert.Equal(full, stable.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_DroppedTokenGap_MergesWhenPreviouslySameLine()
    {
        // Frame 1: ... POPULATION III STAR. on one baseline.
        // Frame 2: III drops → ~3×height hole between POPULATION and STAR.
        // Old hysteresis (1.2× wordGap) cannot bridge; SameLineWordGapHysteresis must.
        using var session = CreateSession(
            wordGap: 0.5,
            baselineDrift: 0.5,
            enableStabilization: true,
            sameLineWordGapHysteresis: 6.0);

        var full = await session.ProcessAsync(CreateResult(
            MakeWord("A", 10, 10, 12, 20),
            MakeWord("RELIC", 26, 10, 50, 20),
            MakeWord("LOW-MASS", 80, 10, 70, 20),
            MakeWord("POPULATION", 154, 10, 90, 20),
            MakeWord("III", 248, 10, 24, 20),
            MakeWord("STAR.", 276, 10, 45, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(full.Paragraphs);
        Assert.Single(full.Paragraphs[0].Lines);
        Assert.Equal("A RELIC LOW-MASS POPULATION III STAR.", full.Paragraphs[0].Lines[0].Text);

        var dropped = await session.ProcessAsync(CreateResult(
            MakeWord("A", 10, 10, 12, 20),
            MakeWord("RELIC", 26, 10, 50, 20),
            MakeWord("LOW-MASS", 80, 10, 70, 20),
            MakeWord("POPULATION", 154, 10, 90, 20),
            // III missing: POPULATION ends at 244; STAR at 300 → gap 56 ≈ 2.8×height.
            // Normal maxAlong ≈ 0.5×h×1.2 = 12; same-line 6× → 60 bridges the hole.
            MakeWord("STAR.", 300, 10, 45, 20)
        ), TestContext.Current.CancellationToken);

        // Assembled geometry (ITextResult.Lines), not emitted/ghost paragraphs.
        Assert.Single(dropped.Lines);
        Assert.Equal(5, dropped.Lines[0].Words.Count);
        Assert.DoesNotContain(dropped.Lines[0].Words, w => w.Text == "III");
    }

    [Fact]
    public async Task ProcessAsync_DroppedTokenGap_DoesNotMergeWithoutSameLineHysteresis()
    {
        using var session = CreateSession(
            wordGap: 0.5,
            baselineDrift: 0.5,
            enableStabilization: true,
            paragraphMergeHysteresis: 1.2,
            sameLineWordGapHysteresis: 1.0);

        await session.ProcessAsync(CreateResult(
            MakeWord("POPULATION", 10, 10, 90, 20),
            MakeWord("III", 104, 10, 24, 20),
            MakeWord("STAR.", 132, 10, 45, 20)
        ), TestContext.Current.CancellationToken);

        // Gap POPULATION→STAR = 70 = 3.5×height; wordGap×1.0×h = 10 — must stay split.
        var dropped = await session.ProcessAsync(CreateResult(
            MakeWord("POPULATION", 10, 10, 90, 20),
            MakeWord("STAR.", 170, 10, 45, 20)
        ), TestContext.Current.CancellationToken);

        Assert.True(dropped.Lines.Count >= 2);
    }

    [Fact]
    public async Task ProcessAsync_SmallPoseJitter_SnapsLineBoundsToPrevious()
    {
        using var session = CreateSession(enableStabilization: true);
        const string text = "Hello World";

        var first = await session.ProcessAsync(
            CreateResult(MakeWord(text, 10, 10, 100, 20)), TestContext.Current.CancellationToken);
        Assert.True(first.Lines[0].HasPreviousFrameMatch is false);
        var a = first.Lines[0].Bounds;

        // Translate by a few pixels — same text must freeze rails to the previous frame.
        var second = await session.ProcessAsync(
            CreateResult(MakeWord(text, 13, 12, 100, 20)), TestContext.Current.CancellationToken);
        Assert.True(second.Lines[0].HasPreviousFrameMatch);
        var b = second.Lines[0].Bounds;

        Assert.Equal(a.P5.X, b.P5.X, precision: 1);
        Assert.Equal(a.P5.Y, b.P5.Y, precision: 1);
        Assert.Equal(a.P6.X, b.P6.X, precision: 1);
        Assert.Equal(a.P6.Y, b.P6.Y, precision: 1);
    }

    [Fact]
    public async Task ProcessAsync_HasPreviousFrameMatch_TracksGeometryAcrossTextChanges()
    {
        using var session = CreateSession(enableStabilization: true);

        var first = await session.ProcessAsync(
            CreateResult(MakeWord("Hello World", 10, 10, 100, 20)), TestContext.Current.CancellationToken);
        Assert.False(first.Paragraphs[0].HasPreviousFrameMatch);
        Assert.Equal(1, first.Paragraphs[0].PreviousFrameMatchAge);
        Assert.Equal(string.Empty, first.Paragraphs[0].PreviousFrameText);
        Assert.False(first.Paragraphs[0].Lines[0].HasPreviousFrameMatch);
        Assert.Equal(1, first.Paragraphs[0].Lines[0].PreviousFrameMatchAge);
        Assert.Equal(string.Empty, first.Paragraphs[0].Lines[0].PreviousFrameText);

        var same = await session.ProcessAsync(
            CreateResult(MakeWord("hello world", 10, 10, 100, 20)), TestContext.Current.CancellationToken);
        Assert.True(same.Paragraphs[0].HasPreviousFrameMatch);
        Assert.Equal(2, same.Paragraphs[0].PreviousFrameMatchAge);
        Assert.Equal("Hello World", same.Paragraphs[0].PreviousFrameText);
        Assert.True(same.Paragraphs[0].Lines[0].HasPreviousFrameMatch);
        Assert.Equal(2, same.Paragraphs[0].Lines[0].PreviousFrameMatchAge);
        Assert.Equal("Hello World", same.Paragraphs[0].Lines[0].PreviousFrameText);
        // Old text-equality signal reconstructed from geometry match + PreviousFrameText.
        Assert.Equal(
            same.Paragraphs[0].Lines[0].Text,
            same.Paragraphs[0].Lines[0].PreviousFrameText,
            ignoreCase: true);

        // Same rails, different text — still a previous-frame geometric match.
        var different = await session.ProcessAsync(
            CreateResult(MakeWord("Hello there", 10, 10, 100, 20)), TestContext.Current.CancellationToken);
        Assert.Equal("Hello there", different.Paragraphs[0].Text);
        Assert.True(different.Paragraphs[0].HasPreviousFrameMatch);
        Assert.Equal(3, different.Paragraphs[0].PreviousFrameMatchAge);
        Assert.Equal("Hello World", different.Paragraphs[0].PreviousFrameText);
        Assert.True(different.Lines[0].HasPreviousFrameMatch);
        Assert.Equal(3, different.Lines[0].PreviousFrameMatchAge);
        Assert.Equal("Hello World", different.Lines[0].PreviousFrameText);
        Assert.NotEqual(
            different.Lines[0].Text,
            different.Lines[0].PreviousFrameText,
            StringComparer.OrdinalIgnoreCase);
        Assert.False(different.Paragraphs[0].IsGhost);
        Assert.Equal(0, different.Paragraphs[0].GhostAge);

        // Longer growth on same rails keeps the match and advances age.
        var longer = await session.ProcessAsync(
            CreateResult(MakeWord("Hello World Extended", 10, 10, 160, 20)), TestContext.Current.CancellationToken);
        Assert.Equal("Hello World Extended", longer.Paragraphs[0].Text);
        Assert.True(longer.Lines[0].HasPreviousFrameMatch);
        Assert.Equal(4, longer.Lines[0].PreviousFrameMatchAge);
        Assert.Equal("Hello there", longer.Lines[0].PreviousFrameText);
        Assert.True(longer.Paragraphs[0].HasPreviousFrameMatch);
        Assert.Equal(4, longer.Paragraphs[0].PreviousFrameMatchAge);
        Assert.Equal("Hello there", longer.Paragraphs[0].PreviousFrameText);
    }

    [Fact]
    public async Task ProcessAsync_TiltedStabilization_MatchesAcrossFrames()
    {
        using var session = CreateSession(enableStabilization: true);
        const string text = "Pay heed the frigid blade reveals";

        var first = await session.ProcessAsync(CreateResult(
            MakeOrientedWord(text, 10, 40, 200, 135, 196, 151, 6, 56)
        ), TestContext.Current.CancellationToken);
        Assert.Single(first.Paragraphs);
        Assert.Equal(text, first.Paragraphs[0].Text);
        Assert.False(first.Paragraphs[0].Lines[0].HasPreviousFrameMatch);

        // Slight pose jitter along the same tilt — should keep the first emission.
        var second = await session.ProcessAsync(CreateResult(
            MakeOrientedWord(text, 12, 42, 202, 137, 198, 153, 8, 58)
        ), TestContext.Current.CancellationToken);
        Assert.Single(second.Paragraphs);
        Assert.Equal(text, second.Paragraphs[0].Text);
        Assert.True(second.Paragraphs[0].Lines[0].HasPreviousFrameMatch);
        var a = first.Paragraphs[0].Lines[0].Bounds;
        var b = second.Paragraphs[0].Lines[0].Bounds;
        Assert.Equal(a.AngleDegrees, b.AngleDegrees, precision: 1);
        Assert.InRange(Math.Abs(a.P5.X - b.P5.X), 0, 2);
        Assert.InRange(Math.Abs(a.P5.Y - b.P5.Y), 0, 2);
    }

    [Fact]
    public async Task ProcessAsync_SameLengthVariant_DoesNotRotateEmittedText()
    {
        using var session = CreateSession(enableStabilization: true);
        const string withTypo =
            "I first ended up in this place two months ago so I had a bit of time enemjes";
        const string fixedTypo =
            "I first ended up in this place two months ago so I had a bit of time enemies";

        Assert.Equal(withTypo.Length, fixedTypo.Length);

        var first = await session.ProcessAsync(
            CreateResult(MakeWord(withTypo, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Equal(withTypo, first.Paragraphs[0].Text);

        for (var i = 0; i < 3; i++)
        {
            var frame = await session.ProcessAsync(
                CreateResult(MakeWord(fixedTypo, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
            Assert.Single(frame.Paragraphs);
            Assert.Equal(withTypo, frame.Paragraphs[0].Text);
        }

        var back = await session.ProcessAsync(
            CreateResult(MakeWord(withTypo, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Equal(withTypo, back.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_NewParagraph_HeldUntilSecondFrame()
    {
        using var session = CreateSession(enableStabilization: true, holdNewBlocks: true);
        const string firstText =
            "I first ended up in this place two months ago so I had a bit of time";
        const string secondText =
            "Later another line appears on screen and should wait one frame";

        var frame1 = await session.ProcessAsync(
            CreateResult(MakeWord(firstText, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Empty(frame1.Paragraphs);

        var frame2 = await session.ProcessAsync(
            CreateResult(MakeWord(firstText, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Single(frame2.Paragraphs);
        Assert.Equal(firstText, frame2.Paragraphs[0].Text);

        var frame3 = await session.ProcessAsync(
            CreateResult(
                MakeWord(firstText, 10, 10, 400, 20),
                MakeWord(secondText, 10, 80, 400, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Single(frame3.Paragraphs);
        Assert.Equal(firstText, frame3.Paragraphs[0].Text);

        var frame4 = await session.ProcessAsync(
            CreateResult(
                MakeWord(firstText, 10, 10, 400, 20),
                MakeWord(secondText, 10, 80, 400, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Equal(2, frame4.Paragraphs.Count);
        Assert.Contains(frame4.Paragraphs, p => p.Text == firstText);
        Assert.Contains(frame4.Paragraphs, p => p.Text == secondText);
    }

    [Fact]
    public async Task ProcessAsync_HoldNewBlocksDisabled_EmitsNewParagraphImmediately()
    {
        using var session = CreateSession(enableStabilization: true, holdNewBlocks: false);
        const string firstText =
            "I first ended up in this place two months ago so I had a bit of time";
        const string secondText =
            "Later another line appears on screen and should wait one frame";

        await session.ProcessAsync(
            CreateResult(MakeWord(firstText, 10, 10, 400, 20)), TestContext.Current.CancellationToken);

        var frame2 = await session.ProcessAsync(
            CreateResult(
                MakeWord(firstText, 10, 10, 400, 20),
                MakeWord(secondText, 10, 80, 400, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Equal(2, frame2.Paragraphs.Count);
        Assert.Contains(frame2.Paragraphs, p => p.Text == firstText);
        Assert.Contains(frame2.Paragraphs, p => p.Text == secondText);
    }

    [Fact]
    public async Task ProcessAsync_NewParagraphFlicker_NeverEmitted()
    {
        using var session = CreateSession(enableStabilization: true, holdNewBlocks: true);
        const string stable =
            "I first ended up in this place two months ago so I had a bit of time";
        const string flicker =
            "A one-frame OCR ghost paragraph that should not be emitted";

        Assert.Empty((await session.ProcessAsync(
            CreateResult(MakeWord(stable, 10, 10, 400, 20)), TestContext.Current.CancellationToken)).Paragraphs);

        await session.ProcessAsync(
            CreateResult(MakeWord(stable, 10, 10, 400, 20)), TestContext.Current.CancellationToken);

        var withGhost = await session.ProcessAsync(
            CreateResult(
                MakeWord(stable, 10, 10, 400, 20),
                MakeWord(flicker, 10, 80, 400, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Single(withGhost.Paragraphs);
        Assert.Equal(stable, withGhost.Paragraphs[0].Text);

        var withoutGhost = await session.ProcessAsync(
            CreateResult(MakeWord(stable, 10, 10, 400, 20)), TestContext.Current.CancellationToken);

        Assert.Single(withoutGhost.Paragraphs);
        Assert.Equal(stable, withoutGhost.Paragraphs[0].Text);
        Assert.DoesNotContain(withoutGhost.Paragraphs, p => p.Text == flicker);
    }

    [Fact]
    public async Task ProcessAsync_PartialCenteredLineBelow_HoldsUpperParagraph()
    {
        using var session = CreateSession(
            centerAlign: true,
            enableStabilization: true,
            holdNewBlocks: true);

        // Confirm title first (holdNewBlocks requires two identical frames).
        Assert.Empty((await session.ProcessAsync(
            CreateResult(MakeWord("TitleLine", 150, 10, 100, 20)),
            TestContext.Current.CancellationToken)).Paragraphs);
        var frame1 = await session.ProcessAsync(
            CreateResult(MakeWord("TitleLine", 150, 10, 100, 20)),
            TestContext.Current.CancellationToken);
        Assert.Single(frame1.Paragraphs);
        Assert.Equal("TitleLine", frame1.Paragraphs[0].Text);

        // Incomplete second line under the title — does not merge; title still matches and stays emitted.
        // (Suppress-upper-while-lower-pending is out of scope for v1.)
        var frame2 = await session.ProcessAsync(
            CreateResult(
                MakeWord("TitleLine", 150, 10, 100, 20),
                MakeWord("Pay", 85, 40, 40, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Contains(frame2.Paragraphs, p => p.Text == "TitleLine");

        // Full second line — layout merges into one paragraph. Leading edge still matches the
        // previous title slot, so stabilizer keeps the title one frame while the merge parks.
        const string merged =
            "TitleLine\nPay heed the frigid blade reveals";
        var fullSecond = await session.ProcessAsync(
            CreateResult(
                MakeWord("TitleLine", 150, 10, 100, 20),
                MakeWord("Pay heed the frigid blade reveals", 100, 40, 200, 20)
            ), TestContext.Current.CancellationToken);
        Assert.Single(fullSecond.Paragraphs);
        Assert.Equal("TitleLine", fullSecond.Paragraphs[0].Text);

        var stable = await session.ProcessAsync(
            CreateResult(
                MakeWord("TitleLine", 150, 10, 100, 20),
                MakeWord("Pay heed the frigid blade reveals", 100, 40, 200, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Single(stable.Paragraphs);
        Assert.Equal(merged, stable.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_UnrelatedPendingElsewhere_DoesNotHideStableParagraph()
    {
        using var session = CreateSession(
            enableStabilization: true,
            holdNewBlocks: true);

        const string stable =
            "I first ended up in this place two months ago so I had a bit of time";

        Assert.Empty((await session.ProcessAsync(
            CreateResult(MakeWord(stable, 10, 10, 400, 20)),
            TestContext.Current.CancellationToken)).Paragraphs);
        var frame1 = await session.ProcessAsync(
            CreateResult(MakeWord(stable, 10, 10, 400, 20)),
            TestContext.Current.CancellationToken);
        Assert.Single(frame1.Paragraphs);

        // Flickering OCR junk on the opposite side of the screen, same vertical band.
        var frame2 = await session.ProcessAsync(
            CreateResult(
                MakeWord(stable, 10, 10, 400, 20),
                MakeWord("x", 700, 40, 20, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Single(frame2.Paragraphs);
        Assert.Equal(stable, frame2.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_StabilizationDisabled_EmitsNewAndLongerImmediately()
    {
        using var session = CreateSession(enableStabilization: false);
        const string shorter =
            "I first ended up in this place two months ago so had a bit of time";
        const string full =
            "I first ended up in this place two months ago so I had a bit of time";
        const string secondText =
            "Later another line appears on screen and should wait one frame";

        await session.ProcessAsync(
            CreateResult(MakeWord(shorter, 10, 10, 400, 20)), TestContext.Current.CancellationToken);

        var growing = await session.ProcessAsync(
            CreateResult(
                MakeWord(full, 12, 11, 410, 21),
                MakeWord(secondText, 10, 80, 400, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Equal(2, growing.Paragraphs.Count);
        Assert.Contains(growing.Paragraphs, p => p.Text == full);
        Assert.Contains(growing.Paragraphs, p => p.Text == secondText);
    }

    [Fact]
    public async Task ProcessAsync_ShortDisappear_HidesImmediately()
    {
        using var session = CreateSession(enableStabilization: true, ghostMaxFrames: 0);

        var first = await session.ProcessAsync(
            CreateResult(MakeWord("LV.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Equal("LV.80", first.Paragraphs[0].Text);

        var gone = await session.ProcessAsync(CreateResult(), TestContext.Current.CancellationToken);
        Assert.Empty(gone.Paragraphs);
    }

    [Fact]
    public async Task ProcessAsync_ShortStrongChange_TakesDissimilarCurrent()
    {
        using var session = CreateSession(enableStabilization: true);

        await session.ProcessAsync(
            CreateResult(MakeWord("LV.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);

        // Dissimilar → current wins even on same rails.
        var changed = await session.ProcessAsync(
            CreateResult(MakeWord("HP.12", 12, 11, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Single(changed.Paragraphs);
        Assert.Equal("HP.12", changed.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_ShortStrongChange_ReturnsToOldWithoutBlank()
    {
        using var session = CreateSession(enableStabilization: true);

        await session.ProcessAsync(
            CreateResult(MakeWord("LV.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);

        var flicker = await session.ProcessAsync(
            CreateResult(MakeWord("HP.12", 12, 11, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Equal("HP.12", flicker.Paragraphs[0].Text);

        var back = await session.ProcessAsync(
            CreateResult(MakeWord("LV.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Single(back.Paragraphs);
        Assert.Equal("LV.80", back.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_LongDisappear_HidesImmediately()
    {
        using var session = CreateSession(enableStabilization: true, ghostMaxFrames: 0);
        const string longText =
            "I first ended up in this place two months ago so I had a bit of time";

        await session.ProcessAsync(
            CreateResult(MakeWord(longText, 10, 10, 400, 20)), TestContext.Current.CancellationToken);

        var gone = await session.ProcessAsync(CreateResult(), TestContext.Current.CancellationToken);
        Assert.Empty(gone.Paragraphs);
    }

    [Fact]
    public async Task ProcessAsync_LongStrongChange_TakesDissimilarCurrent()
    {
        using var session = CreateSession(enableStabilization: true);
        const string firstText =
            "I first ended up in this place two months ago so I had a bit of time";
        const string replacement =
            "Completely different dialogue appears in the same on-screen slot now";

        await session.ProcessAsync(
            CreateResult(MakeWord(firstText, 10, 10, 400, 20)), TestContext.Current.CancellationToken);

        var changed = await session.ProcessAsync(
            CreateResult(MakeWord(replacement, 12, 11, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Single(changed.Paragraphs);
        Assert.Equal(replacement, changed.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_ShortGrowsToLong_WaitsUntilTextSettles()
    {
        using var session = CreateSession(enableStabilization: true, holdNewBlocks: true);
        const string shorter = "Hello world";
        const string longer = "Hello world and a growing OCR line";

        Assert.Empty((await session.ProcessAsync(
            CreateResult(MakeWord(shorter, 10, 10, 280, 20)), TestContext.Current.CancellationToken)).Paragraphs);

        var first = await session.ProcessAsync(
            CreateResult(MakeWord(shorter, 10, 10, 280, 20)), TestContext.Current.CancellationToken);
        Assert.Equal(shorter, first.Paragraphs[0].Text);

        // Growing: previous was shown but originals differ beyond fuzzy threshold → hold; ghost keeps shorter.
        var growing = await session.ProcessAsync(
            CreateResult(MakeWord(longer, 10, 10, 280, 20)), TestContext.Current.CancellationToken);
        Assert.Single(growing.Paragraphs);
        Assert.Equal(shorter, growing.Paragraphs[0].Text);

        // Second identical longer frame → exact original match → emit longer.
        var settled = await session.ProcessAsync(
            CreateResult(MakeWord(longer, 10, 10, 280, 20)), TestContext.Current.CancellationToken);
        Assert.Single(settled.Paragraphs);
        Assert.Equal(longer, settled.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_HoldNewBlocks_GrowingTypewriter_WaitsUntilTextSettles()
    {
        using var session = CreateSession(enableStabilization: true, holdNewBlocks: true);
        const string a = "Hi";
        const string b = "Hi there";
        const string c = "Hi there friend this line is done";

        Assert.Empty((await session.ProcessAsync(
            CreateResult(MakeWord(a, 10, 10, 40, 20)), TestContext.Current.CancellationToken)).Paragraphs);

        // Different text while previous was never shown → still hold.
        Assert.Empty((await session.ProcessAsync(
            CreateResult(MakeWord(b, 10, 10, 100, 20)), TestContext.Current.CancellationToken)).Paragraphs);

        // Second identical mid frame → emit.
        var mid = await session.ProcessAsync(
            CreateResult(MakeWord(b, 10, 10, 100, 20)), TestContext.Current.CancellationToken);
        Assert.Single(mid.Paragraphs);
        Assert.Equal(b, mid.Paragraphs[0].Text);

        // Grow further → hold current, ghost keeps mid.
        var growing = await session.ProcessAsync(
            CreateResult(MakeWord(c, 10, 10, 280, 20)), TestContext.Current.CancellationToken);
        Assert.Single(growing.Paragraphs);
        Assert.Equal(b, growing.Paragraphs[0].Text);

        var full = await session.ProcessAsync(
            CreateResult(MakeWord(c, 10, 10, 280, 20)), TestContext.Current.CancellationToken);
        Assert.Single(full.Paragraphs);
        Assert.Equal(c, full.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_HoldNewBlocks_SkipsIntermediatePause_WhenGrowthContinues()
    {
        using var session = CreateSession(enableStabilization: true, holdNewBlocks: true);
        const string mid = "Hi there";
        const string full = "Hi there friend this line is done";

        Assert.Empty((await session.ProcessAsync(
            CreateResult(MakeWord(mid, 10, 10, 100, 20)), TestContext.Current.CancellationToken)).Paragraphs);

        // Jump to full before mid was shown → hold (mid never locked in).
        Assert.Empty((await session.ProcessAsync(
            CreateResult(MakeWord(full, 10, 10, 280, 20)), TestContext.Current.CancellationToken)).Paragraphs);

        var stable = await session.ProcessAsync(
            CreateResult(MakeWord(full, 10, 10, 280, 20)), TestContext.Current.CancellationToken);
        Assert.Single(stable.Paragraphs);
        Assert.Equal(full, stable.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_GrowingText_SameOrigin_TakesLongerImmediately()
    {
        using var session = CreateSession(enableStabilization: true, holdNewBlocks: false);
        const string stub = "Hi";
        const string mid = "Hi there friend";
        const string full =
            "Hi there friend this line keeps growing as OCR catches up";

        var first = await session.ProcessAsync(
            CreateResult(MakeWord(stub, 10, 10, 40, 20)), TestContext.Current.CancellationToken);
        Assert.Equal(stub, first.Paragraphs[0].Text);

        var growing = await session.ProcessAsync(
            CreateResult(MakeWord(mid, 10, 10, 160, 20)), TestContext.Current.CancellationToken);
        Assert.Single(growing.Paragraphs);
        Assert.Equal(mid, growing.Paragraphs[0].Text);

        var toFull = await session.ProcessAsync(
            CreateResult(MakeWord(full, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Equal(full, toFull.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_HoldNewBlocksOff_ShorterDissimilar_TakesCurrent()
    {
        using var session = CreateSession(enableStabilization: true, holdNewBlocks: false);
        const string firstText =
            "I first ended up in this place two months ago so I had a bit of time";
        const string replacement = "Hi";

        await session.ProcessAsync(
            CreateResult(MakeWord(firstText, 10, 10, 400, 20)), TestContext.Current.CancellationToken);

        var changed = await session.ProcessAsync(
            CreateResult(MakeWord(replacement, 10, 10, 40, 20)), TestContext.Current.CancellationToken);
        Assert.Single(changed.Paragraphs);
        Assert.Equal(replacement, changed.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_ParagraphHysteresis_KeepsMergedWhenGapGrowsSlightly()
    {
        // lineSpacing 1.5 * h20 → max gap 30. Frame1 merges (gap 28); frame2 gap 32
        // exceeds default but PreferMerge with 1.2 → max 36 keeps one paragraph.
        using var session = CreateSession(
            enableStabilization: false,
            paragraphMergeHysteresis: 1.2);

        var frame1 = await session.ProcessAsync(
            CreateResult(
                MakeWord("What Burns Beneath", 10, 10, 200, 20),
                MakeWord("Frostlands", 10, 38, 120, 20)
            ), TestContext.Current.CancellationToken);
        Assert.Single(frame1.Paragraphs);
        Assert.Equal("What Burns Beneath\nFrostlands", frame1.Paragraphs[0].Text);

        var frame2 = await session.ProcessAsync(
            CreateResult(
                MakeWord("What Burns Beneath", 10, 10, 200, 20),
                MakeWord("Frostlands", 10, 42, 120, 20)
            ), TestContext.Current.CancellationToken);
        Assert.Single(frame2.Paragraphs);
        Assert.Equal("What Burns Beneath\nFrostlands", frame2.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_ParagraphHysteresis_KeepsSplitWhenGapShrinksSlightly()
    {
        // Frame1: gap 40 > 30 → two paragraphs. Frame2: gap 28 would merge by default,
        // but PreferSplit with 1.2 → max 25 keeps them apart.
        using var session = CreateSession(
            enableStabilization: false,
            paragraphMergeHysteresis: 1.2);

        var frame1 = await session.ProcessAsync(
            CreateResult(
                MakeWord("Head to the Artificial Sun Lab", 10, 10, 300, 20),
                MakeWord("Head to Starward Riseway", 10, 50, 260, 20)
            ), TestContext.Current.CancellationToken);
        Assert.Equal(2, frame1.Paragraphs.Count);

        var frame2 = await session.ProcessAsync(
            CreateResult(
                MakeWord("Head to the Artificial Sun Lab", 10, 10, 300, 20),
                MakeWord("Head to Starward Riseway", 10, 38, 260, 20)
            ), TestContext.Current.CancellationToken);
        Assert.Equal(2, frame2.Paragraphs.Count);
    }

    [Fact]
    public async Task ProcessAsync_ParagraphHysteresisDisabled_AllowsMergeFlip()
    {
        using var session = CreateSession(
            enableStabilization: false,
            paragraphMergeHysteresis: 1.0);

        await session.ProcessAsync(
            CreateResult(
                MakeWord("Head to the Artificial Sun Lab", 10, 10, 300, 20),
                MakeWord("Head to Starward Riseway", 10, 50, 260, 20)
            ), TestContext.Current.CancellationToken);

        var frame2 = await session.ProcessAsync(
            CreateResult(
                MakeWord("Head to the Artificial Sun Lab", 10, 10, 300, 20),
                MakeWord("Head to Starward Riseway", 10, 35, 260, 20)
            ), TestContext.Current.CancellationToken);
        Assert.Single(frame2.Paragraphs);
    }

    [Fact]
    public async Task ProcessAsync_WordFilterSkip_RemovesWordBeforeLineMerge()
    {
        var filter = LayoutTextFilter.FromRules([("noise", IsRegex: false, Strip: false)]);
        using var session = CreateSession(wordFilter: filter);

        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Hello", 10, 10, 45, 20),
            MakeWord("World", 59, 10, 45, 20),
            MakeWord("noise", 10, 80, 40, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal("Hello World", result.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_WordFilterStripRegex_RemovesMatchFromWord()
    {
        var filter = LayoutTextFilter.FromRules([(@"\d+m", IsRegex: true, Strip: true)]);
        using var session = CreateSession(wordFilter: filter);

        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Target", 10, 10, 50, 20),
            MakeWord("120m", 70, 10, 40, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal("Target", result.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_LineFilterSkip_DropsWholeLine()
    {
        var filter = LayoutTextFilter.FromRules([("Head to Starward Riseway", IsRegex: false, Strip: false)]);
        using var session = CreateSession(lineSpacing: 0.5, lineFilter: filter);

        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Head to the Artificial Sun Lab", 10, 10, 300, 20),
            MakeWord("Head to Starward Riseway", 10, 50, 260, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal("Head to the Artificial Sun Lab", result.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_ParagraphFilterStripRegex_RemovesDistanceNoise()
    {
        var filter = LayoutTextFilter.FromRules([(@"\s*\d+\s*m\b", IsRegex: true, Strip: true)]);
        using var session = CreateSession(paragraphFilter: filter);

        var result = await session.ProcessAsync(CreateResult(
            MakeWord("Head to the Artificial Sun Lab 42 m", 10, 10, 400, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal("Head to the Artificial Sun Lab", result.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_WordFilterNonRegex_RequiresExactMatch()
    {
        var filter = LayoutTextFilter.FromRules([("noise", IsRegex: false, Strip: false)]);
        using var session = CreateSession(wordFilter: filter);

        var result = await session.ProcessAsync(CreateResult(
            MakeWord("noisy", 10, 10, 45, 20)
        ), TestContext.Current.CancellationToken);

        Assert.Single(result.Paragraphs);
        Assert.Equal("noisy", result.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_StableIds_ReusePreviousLineAndParagraphIds()
    {
        using var session = CreateSession(enableStabilization: true, holdNewBlocks: false);
        const string text = "Stable identity line for tracking";

        var first = await session.ProcessAsync(
            CreateResult(MakeWord(text, 10, 10, 300, 20)), TestContext.Current.CancellationToken);
        Assert.Single(first.Paragraphs);
        Assert.Single(first.Paragraphs[0].Lines);
        var paragraphId = first.Paragraphs[0].Id;
        var lineId = first.Paragraphs[0].Lines[0].Id;
        Assert.NotEqual(Guid.Empty, paragraphId);
        Assert.NotEqual(Guid.Empty, lineId);

        var second = await session.ProcessAsync(
            CreateResult(MakeWord(text, 12, 11, 300, 20)), TestContext.Current.CancellationToken);
        Assert.Single(second.Paragraphs);
        Assert.Single(second.Paragraphs[0].Lines);
        Assert.Equal(paragraphId, second.Paragraphs[0].Id);
        Assert.Equal(lineId, second.Paragraphs[0].Lines[0].Id);
        Assert.True(second.Paragraphs[0].HasPreviousFrameMatch);
        Assert.Equal(2, second.Paragraphs[0].PreviousFrameMatchAge);
        Assert.True(second.Paragraphs[0].Lines[0].HasPreviousFrameMatch);
        Assert.Equal(2, second.Paragraphs[0].Lines[0].PreviousFrameMatchAge);

        var third = await session.ProcessAsync(
            CreateResult(MakeWord("Completely different block elsewhere", 10, 200, 300, 20)),
            TestContext.Current.CancellationToken);
        // Previous block may remain as a ghost; the new live paragraph must get fresh ids.
        var live = Assert.Single(third.Paragraphs, p => p is TextParagraph tp && !tp.IsGhost);
        Assert.NotEqual(paragraphId, live.Id);
        Assert.NotEqual(lineId, live.Lines[0].Id);
        Assert.False(live.HasPreviousFrameMatch);
        Assert.Equal(1, live.PreviousFrameMatchAge);
        var ghost = Assert.Single(third.Paragraphs.OfType<TextParagraph>(), p => p.IsGhost);
        Assert.Equal(paragraphId, ghost.Id);
        Assert.Equal(lineId, ghost.Lines[0].Id);
        Assert.True(ghost.IsGhost);
        Assert.Equal(1, ghost.GhostAge);
        Assert.True(ghost.HasPreviousFrameMatch);
        Assert.Equal(3, ghost.PreviousFrameMatchAge);
    }

    private sealed class StubWord : IOCRWord
    {
        public string Text { get; }
        public BoundingBox Bounds { get; }
        public double Confidence { get; }

        public StubWord(string text, BoundingBox bounds, double confidence)
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
