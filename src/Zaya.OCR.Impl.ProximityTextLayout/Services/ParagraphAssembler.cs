using System.Numerics;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Groups snapped lines into paragraphs using overlap geometry and PreviousFrameLineList neighbors.
/// </summary>
internal sealed class ParagraphAssembler
{
    private readonly ProximityTextLayoutOptions _options;
    private readonly double _hysteresis;

    public ParagraphAssembler(ProximityTextLayoutOptions options)
    {
        _options = options;
        _hysteresis = Math.Clamp(options.ParagraphMergeHysteresis, 1.0, 3.0);
    }

    public void Assemble(TextResult frame, TextLayoutHistoryService history)
    {
        var lines = frame.MutableLines;
        if (lines.Count == 0)
        {
            frame.MutableParagraphs = [];
            return;
        }

        // Index prev → current for reverse lookup (detect duplicate claims).
        var prevToCurrent = new Dictionary<TextLine, TextLine>();
        var ambiguousPrev = new HashSet<TextLine>();
        foreach (var line in lines)
        {
            foreach (var prev in line.PreviousFrameLineList)
            {
                if (prevToCurrent.TryGetValue(prev, out var existing) && !ReferenceEquals(existing, line))
                {
                    ambiguousPrev.Add(prev);
                    continue;
                }

                prevToCurrent[prev] = line;
            }
        }

        foreach (var prev in ambiguousPrev)
            prevToCurrent.Remove(prev);

        TextLine? FindCurrentForPrev(TextLine? want)
        {
            if (want is null || ambiguousPrev.Contains(want))
                return null;
            return prevToCurrent.GetValueOrDefault(want);
        }

        var buckets = new List<List<TextLine>>();
        var assigned = new HashSet<TextLine>();

        foreach (var line in lines)
        {
            if (assigned.Contains(line))
                continue;

            var bucket = new List<TextLine> { line };
            assigned.Add(line);

            // Grow downward via temporal neighbor then geometry.
            while (true)
            {
                var last = bucket[^1];
                TextLine? next = null;

                if (last.PreviousFrameLineList.Count > 0)
                {
                    var right = last.PreviousFrameLineList[^1];
                    next = FindCurrentForPrev(right.NextLine);
                    if (next is not null && assigned.Contains(next))
                        next = null;
                    if (next is not null && !CanMergeLines(bucket, next, PreferScale(last, next, preferMerge: true)))
                        next = null;
                }

                if (next is null)
                {
                    // Nearest unassigned line below that passes geometry (do not skip over a closer line).
                    next = lines
                        .Where(c => !assigned.Contains(c))
                        .Select(c =>
                        {
                            var lastCenter = (last.Bounds.P7 + last.Bounds.P8) * 0.5f;
                            var cCenter = (c.Bounds.P7 + c.Bounds.P8) * 0.5f;
                            var dist = Vector2.Dot(cCenter - lastCenter, last.Bounds.Normal);
                            return (Line: c, Dist: dist);
                        })
                        .Where(x => x.Dist >= -0.25 * Math.Max(1.0, last.Bounds.TextHeight))
                        .OrderBy(x => x.Dist)
                        .Select(x => x.Line)
                        .FirstOrDefault(c =>
                            CanMergeLines(bucket, c, GetMergeScale(last, c, FindCurrentForPrev)));
                }

                if (next is null)
                    break;

                bucket.Add(next);
                assigned.Add(next);
            }

            buckets.Add(bucket);
        }

        // Attach unassigned (should be none) as singletons.
        foreach (var line in lines)
        {
            if (assigned.Contains(line))
                continue;
            buckets.Add([line]);
        }

        var paragraphs = new List<TextParagraph>();
        foreach (var bucket in buckets)
        {
            for (var i = 0; i < bucket.Count; i++)
            {
                bucket[i].PrevLine = i > 0 ? bucket[i - 1] : null;
                bucket[i].NextLine = i + 1 < bucket.Count ? bucket[i + 1] : null;
            }

            var text = string.Join("\n", bucket.Select(l => l.Text));
            paragraphs.Add(new TextParagraph(text, bucket));
        }

        frame.MutableParagraphs = paragraphs;
    }

    private double GetMergeScale(
        TextLine upper,
        TextLine lower,
        Func<TextLine?, TextLine?> findCurrentForPrev)
    {
        if (_hysteresis <= 1.0001)
            return 1.0;

        if (TemporalPreferMerge(upper, lower, findCurrentForPrev))
            return _hysteresis;

        if (TemporalPreferSplit(upper, lower))
            return 1.0 / _hysteresis;

        return 1.0;
    }

    private static bool TemporalPreferSplit(TextLine upper, TextLine lower)
    {
        if (upper.PreviousFrameLineList.Count == 0 || lower.PreviousFrameLineList.Count == 0)
            return false;

        // Different previous paragraphs (via PrevLine/NextLine chain roots) → prefer split.
        // Heuristic: if neither end links to the other previous line, treat as separate.
        var u = upper.PreviousFrameLineList[0];
        var l = lower.PreviousFrameLineList[0];
        if (ReferenceEquals(u, l))
            return false;
        if (ReferenceEquals(u.NextLine, l) || ReferenceEquals(u.PrevLine, l)
            || ReferenceEquals(l.NextLine, u) || ReferenceEquals(l.PrevLine, u))
            return false;

        // Walk: if they share a connected Prev/Next chain, same paragraph.
        var seen = new HashSet<TextLine>();
        var q = new Queue<TextLine>();
        q.Enqueue(u);
        while (q.Count > 0)
        {
            var n = q.Dequeue();
            if (!seen.Add(n))
                continue;
            if (ReferenceEquals(n, l))
                return false;
            if (n.PrevLine is not null) q.Enqueue(n.PrevLine);
            if (n.NextLine is not null) q.Enqueue(n.NextLine);
        }

        return true;
    }

    private double PreferScale(TextLine upper, TextLine lower, bool preferMerge)
    {
        if (_hysteresis <= 1.0001)
            return 1.0;
        return preferMerge ? _hysteresis : 1.0 / _hysteresis;
    }

    private bool TemporalPreferMerge(
        TextLine upper,
        TextLine lower,
        Func<TextLine?, TextLine?> findCurrentForPrev)
    {
        if (upper.PreviousFrameLineList.Count == 0 || lower.PreviousFrameLineList.Count == 0)
            return false;

        var wantBelow = upper.PreviousFrameLineList[^1].NextLine;
        var mapped = findCurrentForPrev(wantBelow);
        return mapped is not null && ReferenceEquals(mapped, lower);
    }

    private bool CanMergeLines(List<TextLine> bucket, TextLine line, double scale)
    {
        var lastLine = bucket[^1];
        var avgHeight = Math.Max(1.0, (lastLine.Bounds.TextHeight + line.Bounds.TextHeight) / 2.0);

        if (AngleDeltaDegrees(lastLine.Bounds.AngleDegrees, line.Bounds.AngleDegrees)
            > _options.AngleToleranceDegrees)
            return false;

        var normal = lastLine.Bounds.Normal;
        // Center-to-center distance along paragraph normal (NOT empty gap between boxes).
        // For h=20, boxes y=10..30 and y=40..60 → centers 20 and 50 → spacing=30
        // (= 1.0×height between baselines≈centers + visual gap 10).
        var lastCenter = (lastLine.Bounds.P7 + lastLine.Bounds.P8) * 0.5f;
        var newCenter = (line.Bounds.P7 + line.Bounds.P8) * 0.5f;
        var spacing = Vector2.Dot(newCenter - lastCenter, normal);

        if (spacing < -0.25 * avgHeight)
            return false;

        // LineSpacingThreshold is a multiplier of avg line height for max center-to-center spacing.
        var maxSpacing = _options.LineSpacingThreshold * avgHeight * scale;
        if (spacing > maxSpacing + 0.5)
            return false;

        var heightDiff = Math.Abs(line.Bounds.TextHeight - lastLine.Bounds.TextHeight);
        var maxHeightDiff = _options.FontSizeTolerance * avgHeight * scale;
        if (heightDiff > maxHeightDiff)
            return false;

        return HasAlongOverlap(lastLine, line, scale);
    }

    private bool HasAlongOverlap(TextLine a, TextLine b, double scale)
    {
        var dir = a.Bounds.Direction;
        if (AngleDeltaDegrees(a.Bounds.AngleDegrees, b.Bounds.AngleDegrees) > _options.AngleToleranceDegrees)
            return false;

        var a0 = Vector2.Dot(a.Bounds.P5, dir);
        var a1 = Vector2.Dot(a.Bounds.P6, dir);
        var b0 = Vector2.Dot(b.Bounds.P5, dir);
        var b1 = Vector2.Dot(b.Bounds.P6, dir);
        if (a0 > a1) (a0, a1) = (a1, a0);
        if (b0 > b1) (b0, b1) = (b1, b0);

        var lenA = Math.Max(1e-3, a1 - a0);
        var lenB = Math.Max(1e-3, b1 - b0);
        var overlap = Math.Max(0, Math.Min(a1, b1) - Math.Max(a0, b0));

        // PreferMerge (scale>1) must loosen: allow more protrusion. PreferSplit tightens.
        var maxProtrusion = Math.Clamp(_options.MaxLineProtrusionFraction * scale, 0.02, 0.5);
        var minCoverage = 1.0 - maxProtrusion;
        if (overlap >= minCoverage * lenA && overlap >= minCoverage * lenB)
            return true;

        // Fallback: left-edge alignment.
        var maxLateral = _options.LeftEdgeAlignmentTolerance * Math.Max(a.Bounds.TextHeight, b.Bounds.TextHeight) * scale;
        var leftDiff = Math.Abs(Vector2.Dot(b.Bounds.P1 - a.Bounds.P1, dir));
        if (leftDiff <= maxLateral)
            return true;

        // Fallback: first-line indent (upper starts to the right of lower).
        var indent = Vector2.Dot(a.Bounds.P1 - b.Bounds.P1, dir);
        if (indent > 0
            && indent <= _options.FirstLineIndentTolerance * Math.Max(a.Bounds.TextHeight, b.Bounds.TextHeight) * scale)
            return true;

        if (_options.EnableCenterAlignment)
        {
            var centerDiff = Math.Abs(Vector2.Dot(b.Bounds.P7 - a.Bounds.P7, dir));
            if (centerDiff <= maxLateral)
                return true;
        }

        return false;
    }

    private static double AngleDeltaDegrees(double a, double b)
    {
        var d = Math.Abs(a - b) % 360.0;
        if (d > 180.0)
            d = 360.0 - d;
        return d;
    }
}
