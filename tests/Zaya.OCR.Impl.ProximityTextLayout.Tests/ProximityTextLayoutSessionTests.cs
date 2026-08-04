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
        bool enableStabilization = false,
        double paragraphMergeHysteresis = 1.2,
        LayoutTextFilter? wordFilter = null,
        LayoutTextFilter? lineFilter = null,
        LayoutTextFilter? paragraphFilter = null)
    {
        var options = new ProximityTextLayoutOptions(
            wordGap, baselineDrift, lineSpacing, leftEdgeAlign, firstLineIndent, centerAlign, fontSizeTolerance,
            EnableStabilization: enableStabilization,
            ParagraphMergeHysteresis: paragraphMergeHysteresis);
        return new ProximityTextLayoutSession(options, wordFilter, lineFilter, paragraphFilter);
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
        Assert.Equal(first.Paragraphs[0].Lines[0].Bounds, second.Paragraphs[0].Lines[0].Bounds);
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
        Assert.Equal(shorter, growing.Paragraphs[0].Text);

        var stable = await session.ProcessAsync(
            CreateResult(MakeWord(full, 12, 11, 410, 21)), TestContext.Current.CancellationToken);
        Assert.Single(stable.Paragraphs);
        Assert.Equal(full, stable.Paragraphs[0].Lines[0].Text);
        Assert.Equal(new Rectangle(12, 11, 410, 21), stable.Paragraphs[0].Lines[0].Bounds);
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
        using var session = CreateSession(enableStabilization: true);
        const string firstText =
            "I first ended up in this place two months ago so I had a bit of time";
        const string secondText =
            "Later another line appears on screen and should wait one frame";

        var frame1 = await session.ProcessAsync(
            CreateResult(MakeWord(firstText, 10, 10, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Single(frame1.Paragraphs);

        var frame2 = await session.ProcessAsync(
            CreateResult(
                MakeWord(firstText, 10, 10, 400, 20),
                MakeWord(secondText, 10, 80, 400, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Single(frame2.Paragraphs);
        Assert.Equal(firstText, frame2.Paragraphs[0].Text);

        var frame3 = await session.ProcessAsync(
            CreateResult(
                MakeWord(firstText, 10, 10, 400, 20),
                MakeWord(secondText, 10, 80, 400, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Equal(2, frame3.Paragraphs.Count);
        Assert.Contains(frame3.Paragraphs, p => p.Text == firstText);
        Assert.Contains(frame3.Paragraphs, p => p.Text == secondText);
    }

    [Fact]
    public async Task ProcessAsync_NewParagraphFlicker_NeverEmitted()
    {
        using var session = CreateSession(enableStabilization: true);
        const string stable =
            "I first ended up in this place two months ago so I had a bit of time";
        const string flicker =
            "A one-frame OCR ghost paragraph that should not be emitted";

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
            enableStabilization: true);

        // Full centered title (center ~200).
        var frame1 = await session.ProcessAsync(
            CreateResult(MakeWord("TitleLine", 150, 10, 100, 20)),
            TestContext.Current.CancellationToken);
        Assert.Single(frame1.Paragraphs);
        Assert.Equal("TitleLine", frame1.Paragraphs[0].Text);

        // Incomplete second line under the title (centers/left edges won't merge in layout,
        // but the lower center sits under the upper box → hold the title).
        var frame2 = await session.ProcessAsync(
            CreateResult(
                MakeWord("TitleLine", 150, 10, 100, 20),
                MakeWord("Pay", 100, 40, 40, 20)
            ), TestContext.Current.CancellationToken);

        Assert.Empty(frame2.Paragraphs);

        // Full second line — centers align, layout merges into one paragraph.
        const string merged =
            "TitleLine\nPay heed the frigid blade reveals";
        var fullSecond = await session.ProcessAsync(
            CreateResult(
                MakeWord("TitleLine", 150, 10, 100, 20),
                MakeWord("Pay heed the frigid blade reveals", 100, 40, 200, 20)
            ), TestContext.Current.CancellationToken);
        Assert.Empty(fullSecond.Paragraphs); // first sight of merged block — pending

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
            enableStabilization: true);

        const string stable =
            "I first ended up in this place two months ago so I had a bit of time";

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
    public async Task ProcessAsync_ShortDisappear_GhostsOneFrameThenHides()
    {
        using var session = CreateSession(enableStabilization: true);

        var first = await session.ProcessAsync(
            CreateResult(MakeWord("LV.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Equal("LV.80", first.Paragraphs[0].Text);

        var ghost = await session.ProcessAsync(CreateResult(), TestContext.Current.CancellationToken);
        Assert.Single(ghost.Paragraphs);
        Assert.Equal("LV.80", ghost.Paragraphs[0].Text);

        var gone = await session.ProcessAsync(CreateResult(), TestContext.Current.CancellationToken);
        Assert.Empty(gone.Paragraphs);
    }

    [Fact]
    public async Task ProcessAsync_ShortStrongChange_GhostsOldThenShowsNew()
    {
        using var session = CreateSession(enableStabilization: true);

        await session.ProcessAsync(
            CreateResult(MakeWord("LV.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);

        var changed = await session.ProcessAsync(
            CreateResult(MakeWord("HP.12", 12, 11, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Single(changed.Paragraphs);
        Assert.Equal("LV.80", changed.Paragraphs[0].Text);

        var stable = await session.ProcessAsync(
            CreateResult(MakeWord("HP.12", 12, 11, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Single(stable.Paragraphs);
        Assert.Equal("HP.12", stable.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_ShortStrongChange_ReturnsToOldWithoutBlank()
    {
        using var session = CreateSession(enableStabilization: true);

        await session.ProcessAsync(
            CreateResult(MakeWord("LV.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);

        var flicker = await session.ProcessAsync(
            CreateResult(MakeWord("HP.12", 12, 11, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Equal("LV.80", flicker.Paragraphs[0].Text);

        var back = await session.ProcessAsync(
            CreateResult(MakeWord("LV.80", 10, 10, 50, 20)), TestContext.Current.CancellationToken);
        Assert.Single(back.Paragraphs);
        Assert.Equal("LV.80", back.Paragraphs[0].Text);
    }

    [Fact]
    public async Task ProcessAsync_LongDisappear_HidesImmediately()
    {
        using var session = CreateSession(enableStabilization: true);
        const string longText =
            "I first ended up in this place two months ago so I had a bit of time";

        await session.ProcessAsync(
            CreateResult(MakeWord(longText, 10, 10, 400, 20)), TestContext.Current.CancellationToken);

        var gone = await session.ProcessAsync(CreateResult(), TestContext.Current.CancellationToken);
        Assert.Empty(gone.Paragraphs);
    }

    [Fact]
    public async Task ProcessAsync_LongStrongChange_GhostsOldThenShowsNew()
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
        Assert.Equal(firstText, changed.Paragraphs[0].Text);

        var stable = await session.ProcessAsync(
            CreateResult(MakeWord(replacement, 12, 11, 400, 20)), TestContext.Current.CancellationToken);
        Assert.Single(stable.Paragraphs);
        Assert.Equal(replacement, stable.Paragraphs[0].Text);
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
                MakeWord("Head to Starward Riseway", 10, 38, 260, 20)
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
