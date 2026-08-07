using Zaya.OCR.Impl.ProximityTextLayout.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Decides which paragraphs to emit and with which display text.
/// </summary>
internal sealed class ParagraphTextEmitter
{
    private readonly ProximityTextLayoutOptions _options;

    public ParagraphTextEmitter(ProximityTextLayoutOptions options)
    {
        _options = options;
    }

    public void Emit(TextResult frame, TextLayoutHistoryService history)
    {
        foreach (var paragraph in frame.MutableParagraphs)
        {
            var currDisplay = JoinParagraphDisplay(paragraph);

            if (!_options.EnableStabilization)
            {
                paragraph.Text = currDisplay;
                MarkShown(paragraph);
                continue;
            }

            var hasPrev = TryGetPreviousParagraphs(
                paragraph,
                history,
                out var prevParagraphs,
                out var prevOriginal,
                out var prevDisplay);

            if (!_options.HoldNewBlocks)
            {
                EmitWithoutHold(paragraph, currDisplay, hasPrev, prevDisplay);
                continue;
            }

            // HoldNewBlocks: show only when linked to a previous paragraph and text has settled
            // (exact original match) or the previous was already shown and normalized text is similar.
            if (!hasPrev)
            {
                Hold(paragraph, currDisplay);
                continue;
            }

            var exactOriginal = string.Equals(
                paragraph.OriginalText,
                prevOriginal,
                StringComparison.Ordinal);

            var prevWasShown = prevParagraphs.TrueForAll(p => p.WasShown);
            var normalizedSimilar = prevWasShown
                && TextNormalize.FuzzyMatch(
                    TextNormalize.CompareKey(paragraph.OriginalText),
                    TextNormalize.CompareKey(prevOriginal),
                    _options.LevenshteinThresholdPercent);

            if (!exactOriginal && !normalizedSimilar)
            {
                Hold(paragraph, currDisplay);
                continue;
            }

            EmitLinked(paragraph, currDisplay, prevDisplay);
        }
    }

    private void EmitWithoutHold(
        TextParagraph paragraph,
        string currDisplay,
        bool hasPrev,
        string prevDisplay)
    {
        if (hasPrev)
        {
            EmitLinked(paragraph, currDisplay, prevDisplay);
            return;
        }

        paragraph.Text = currDisplay;
        MarkShown(paragraph);
    }

    private void EmitLinked(TextParagraph paragraph, string currDisplay, string prevDisplay)
    {
        var similar = TextNormalize.FuzzyMatch(
            TextNormalize.CompareKey(currDisplay),
            TextNormalize.CompareKey(prevDisplay),
            _options.LevenshteinThresholdPercent);

        // Similar → longer wins; equal length keeps previous.
        // Dissimilar (strong change on same rails) → take current.
        var chosen = similar ? ChooseByLength(prevDisplay, currDisplay) : currDisplay;
        ApplyChosenText(paragraph, chosen, prevDisplay);
        MarkShown(paragraph);
    }

    private static void Hold(TextParagraph paragraph, string currDisplay)
    {
        paragraph.Text = currDisplay;
        paragraph.IsEmitted = false;
        paragraph.WasShown = false;
        paragraph.IsGhost = false;
    }

    private static void MarkShown(TextParagraph paragraph)
    {
        paragraph.IsEmitted = true;
        paragraph.WasShown = true;
        paragraph.IsGhost = false;
        paragraph.GhostAge = 0;
    }

    /// <summary>
    /// Resolves previous-frame paragraph(s) linked via <see cref="TextLine.PreviousFrameLineList"/>.
    /// </summary>
    private static bool TryGetPreviousParagraphs(
        TextParagraph paragraph,
        TextLayoutHistoryService history,
        out List<TextParagraph> previousParagraphs,
        out string previousOriginal,
        out string previousDisplay)
    {
        previousParagraphs = [];
        previousOriginal = string.Empty;
        previousDisplay = string.Empty;

        if (history.Previous is null || paragraph.TextLines.Count == 0)
            return false;

        var lineToParagraph = new Dictionary<TextLine, TextParagraph>();
        foreach (var prevParagraph in history.Previous.AllParagraphs)
        {
            foreach (var line in prevParagraph.TextLines)
                lineToParagraph[line] = prevParagraph;
        }

        var ordered = new List<TextParagraph>();
        var displayParts = new List<string>();

        foreach (var line in paragraph.TextLines)
        {
            if (line.PreviousFrameLineList.Count == 0)
                return false;

            displayParts.Add(string.Join(" ", line.PreviousFrameLineList.Select(p => p.Text)));

            foreach (var prevLine in line.PreviousFrameLineList)
            {
                if (!lineToParagraph.TryGetValue(prevLine, out var prevParagraph))
                    return false;

                if (!ordered.Contains(prevParagraph))
                    ordered.Add(prevParagraph);
            }
        }

        previousParagraphs = ordered;
        previousOriginal = string.Join("\n", ordered.Select(p => p.OriginalText));
        previousDisplay = string.Join("\n", displayParts);
        return ordered.Count > 0;
    }

    private static string JoinParagraphDisplay(TextParagraph paragraph)
        => string.Join("\n", paragraph.TextLines.Select(l => l.Text));

    /// <summary>Longer original text wins; equal length keeps <paramref name="previous"/>.</summary>
    private static string ChooseByLength(string previous, string current)
        => current.Length > previous.Length ? current : previous;

    private static void ApplyChosenText(TextParagraph paragraph, string chosen, string previousDisplay)
    {
        paragraph.Text = chosen;

        if (string.Equals(chosen, previousDisplay, StringComparison.Ordinal))
        {
            foreach (var line in paragraph.TextLines)
            {
                if (line.PreviousFrameLineList.Count == 0)
                    continue;
                line.Text = string.Join(" ", line.PreviousFrameLineList.Select(p => p.Text));
            }

            return;
        }

        if (paragraph.TextLines.Count == 1)
            paragraph.TextLines[0].Text = chosen;
    }
}
