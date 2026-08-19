using System.Numerics;
using Zaya.Logging.Services;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.Primitives;
using Zaya.Primitives.OCR;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services.Impl;

/// <summary>
/// Assembles words into lines, then links them to the previous frame (via <see cref="LinePreviousMatcher"/>).
/// </summary>
internal sealed class LineAssembler : ILineAssembler
{
    private readonly ProximityTextLayoutOptions _options;
    private readonly double _lineMergeHysteresis;
    private readonly double _sameLineWordGapHysteresis;
    private readonly ILinePreviousMatcher _previousMatcher;

    public LineAssembler(ProximityTextLayoutOptions options, ILoggingWrapper logging)
    {
        _options = options;
        _lineMergeHysteresis = Math.Clamp(options.ParagraphMergeHysteresis, 1.0, 3.0);
        _sameLineWordGapHysteresis = Math.Clamp(options.SameLineWordGapHysteresis, 1.0, 20.0);
        _previousMatcher = logging.Wrap<ILinePreviousMatcher>(lw => new LinePreviousMatcher(options, lw));
    }
    public void Assemble(TextResult frame, ITextLayoutHistoryService history)
    {
        var words = frame.MutableWords.Cast<IOCRWord>().ToList();
        if (words.Count == 0)
        {
            frame.MutableLines = [];
            return;
        }

        var lines = BuildLines(words, history);
        _previousMatcher.Match(lines, history, snap: _options.EnableStabilization);
        frame.MutableLines = lines;
    }

    private List<TextLine> BuildLines(List<IOCRWord> words, ITextLayoutHistoryService history)
    {
        var remaining = words.ToList();
        remaining.Sort(CompareSeedOrder);

        var lines = new List<TextLine>();
        while (remaining.Count > 0)
        {
            var seed = remaining[0];
            remaining.RemoveAt(0);
            var lineWords = new List<IOCRWord> { seed };

            while (true)
            {
                var appended = TryAppendWord(lineWords, remaining, history);
                var prepended = TryPrependWord(lineWords, remaining, history);
                if (!appended && !prepended)
                    break;
            }

            var sortDir = LineGeometry.AverageDirection(lineWords);
            lineWords.Sort((a, b) =>
            {
                var pa = Vector2.Dot(a.Bounds.P5, sortDir);
                var pb = Vector2.Dot(b.Bounds.P5, sortDir);
                return pa.CompareTo(pb);
            });

            var line = CreateTextLine(lineWords);
            LinkWords(line);
            lines.Add(line);
        }

        return lines;
    }

    private bool TryAppendWord(List<IOCRWord> lineWords, List<IOCRWord> remaining, ITextLayoutHistoryService history)
    {
        var last = lineWords[^1];
        var bestIndex = FindBestNeighborIndex(lineWords, remaining, last.Bounds.P6, append: true, history);
        if (bestIndex < 0)
            return false;

        lineWords.Add(remaining[bestIndex]);
        remaining.RemoveAt(bestIndex);
        return true;
    }

    private bool TryPrependWord(List<IOCRWord> lineWords, List<IOCRWord> remaining, ITextLayoutHistoryService history)
    {
        var first = lineWords[0];
        var bestIndex = FindBestNeighborIndex(lineWords, remaining, first.Bounds.P5, append: false, history);
        if (bestIndex < 0)
            return false;

        lineWords.Insert(0, remaining[bestIndex]);
        remaining.RemoveAt(bestIndex);
        return true;
    }

    private int FindBestNeighborIndex(
        IReadOnlyList<IOCRWord> lineWords,
        List<IOCRWord> remaining,
        Vector2 anchor,
        bool append,
        ITextLayoutHistoryService history)
    {
        var refWord = append ? lineWords[^1] : lineWords[0];
        var dir = refWord.Bounds.Direction;
        var normal = refWord.Bounds.Normal;
        var height = Math.Max(1.0, refWord.Bounds.TextHeight);
        var lineAngle = LineGeometry.AverageAngleDegrees(lineWords);

        var bestIndex = -1;
        var bestScore = double.MaxValue;

        for (var i = 0; i < remaining.Count; i++)
        {
            var candidate = remaining[i];
            if (LineGeometry.AngleDeltaDegrees(lineAngle, candidate.Bounds.AngleDegrees) > _options.AngleToleranceDegrees)
                continue;

            var scaleAlong = 1.0;
            var scaleAcross = 1.0;
            if (history.WereOnSamePreviousLine(refWord.Bounds, candidate.Bounds))
            {
                // Prefer pulling former neighbors across a dropped-word hole along the baseline.
                scaleAlong = _sameLineWordGapHysteresis;
                scaleAcross = _lineMergeHysteresis;
            }
            else
            {
                // PreferMerge across adjacent previous lines (gap where a word dropped).
                var prevA = history.FindPreviousLineCovering(refWord.Bounds);
                var prevB = history.FindPreviousLineCovering(candidate.Bounds);
                if (prevA is not null && prevB is not null && !ReferenceEquals(prevA, prevB)
                    && LineGeometry.AreAdjacentPreviousLines(prevA, prevB))
                {
                    scaleAlong = _lineMergeHysteresis;
                    scaleAcross = _lineMergeHysteresis;
                }
            }

            var maxAlong = _options.WordGapThreshold * height * scaleAlong;
            var maxAcross = _options.BaselineDriftTolerance * height * scaleAcross;

            Vector2 delta = append
                ? candidate.Bounds.P5 - anchor
                : anchor - candidate.Bounds.P6;

            var along = Vector2.Dot(delta, dir);
            var across = Math.Abs(Vector2.Dot(delta, normal));

            if (along < -0.35 * height || along > maxAlong)
                continue;
            if (across > maxAcross)
                continue;

            var score = along >= 0 ? along : 1_000.0 + Math.Abs(along);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestIndex = i;
        }

        return bestIndex;
    }

    private static void LinkWords(TextLine line)
    {
        TextWord? prev = null;
        foreach (var w in line.Words.OfType<TextWord>())
        {
            w.PrevWord = prev;
            if (prev is not null)
                prev.NextWord = w;
            prev = w;
        }

        if (prev is not null)
            prev.NextWord = null;
    }

    private static int CompareSeedOrder(IOCRWord a, IOCRWord b)
    {
        var cmp = a.Bounds.MinY.CompareTo(b.Bounds.MinY);
        return cmp != 0 ? cmp : a.Bounds.MinX.CompareTo(b.Bounds.MinX);
    }

    private static TextLine CreateTextLine(List<IOCRWord> lineWords)
    {
        var text = string.Join(" ", lineWords.Select(w => w.Text));
        var bounds = LineGeometry.CreateLineBounds(lineWords);
        return new TextLine(text, lineWords.ToList(), bounds);
    }
}
