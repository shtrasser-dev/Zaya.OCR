using System.Numerics;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Groups lines into paragraphs by growing each component both ways along the paragraph normal.
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

        foreach (var line in lines)
        {
            line.PrevLine = null;
            line.NextLine = null;
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

        var unassigned = lines.ToHashSet();
        var paragraphs = new List<TextParagraph>();

        // Seed order follows the input list for stable paragraph emission; growth is bidirectional.
        foreach (var seed in lines)
        {
            if (!unassigned.Remove(seed))
                continue;

            var head = seed;
            var tail = seed;

            var grew = true;
            while (grew)
            {
                grew = false;

                var above = FindNeighbor(
                    head,
                    unassigned,
                    below: false,
                    FindCurrentForPrev);
                if (above is not null)
                {
                    unassigned.Remove(above);
                    above.PrevLine = null;
                    above.NextLine = head;
                    head.PrevLine = above;
                    head = above;
                    grew = true;
                }

                var below = FindNeighbor(
                    tail,
                    unassigned,
                    below: true,
                    FindCurrentForPrev);
                if (below is not null)
                {
                    unassigned.Remove(below);
                    below.NextLine = null;
                    below.PrevLine = tail;
                    tail.NextLine = below;
                    tail = below;
                    grew = true;
                }
            }

            var bucket = new List<TextLine>();
            for (var n = head; n is not null; n = n.NextLine)
                bucket.Add(n);

            var text = string.Join("\n", bucket.Select(l => l.Text));
            paragraphs.Add(new TextParagraph(text, bucket));
        }

        AssignParagraphIds(paragraphs, history);
        frame.MutableParagraphs = paragraphs;
    }

    /// <summary>
    /// Nearest unassigned neighbor along <paramref name="anchor"/>'s normal
    /// (below = +Normal, above = −Normal), preferring temporal links when present.
    /// </summary>
    private TextLine? FindNeighbor(
        TextLine anchor,
        HashSet<TextLine> unassigned,
        bool below,
        Func<TextLine?, TextLine?> findCurrentForPrev)
    {
        TextLine? temporal = null;
        if (anchor.PreviousFrameLineList.Count > 0)
        {
            var prevAnchor = below
                ? anchor.PreviousFrameLineList[^1]
                : anchor.PreviousFrameLineList[0];
            var want = below ? prevAnchor.NextLine : prevAnchor.PrevLine;
            temporal = findCurrentForPrev(want);
            if (temporal is not null && !unassigned.Contains(temporal))
                temporal = null;
        }

        if (temporal is not null)
        {
            var (upper, lower) = below ? (anchor, temporal) : (temporal, anchor);
            if (CanMergeOrdered(upper, lower, PreferScale(upper, lower, preferMerge: true)))
                return temporal;
        }

        var height = Math.Max(1.0, anchor.Bounds.TextHeight);
        var normal = anchor.Bounds.Normal;
        var anchorCenter = (anchor.Bounds.P7 + anchor.Bounds.P8) * 0.5f;

        var candidates = unassigned
            .Select(c =>
            {
                var cCenter = (c.Bounds.P7 + c.Bounds.P8) * 0.5f;
                var dist = (double)Vector2.Dot(cCenter - anchorCenter, normal);
                return (Line: c, Dist: dist);
            });

        IEnumerable<(TextLine Line, double Dist)> ordered = below
            ? candidates.Where(x => x.Dist >= -0.25 * height).OrderBy(x => x.Dist)
            : candidates.Where(x => x.Dist <= 0.25 * height).OrderByDescending(x => x.Dist);

        foreach (var (candidate, _) in ordered)
        {
            var (upper, lower) = below ? (anchor, candidate) : (candidate, anchor);
            var scale = GetMergeScale(upper, lower, findCurrentForPrev);
            if (CanMergeOrdered(upper, lower, scale))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Reuses previous-paragraph id and increments match age when every current line maps to
    /// that same previous paragraph; otherwise marks the paragraph as new (age = 1).
    /// </summary>
    private static void AssignParagraphIds(List<TextParagraph> paragraphs, TextLayoutHistoryService history)
    {
        Dictionary<TextLine, TextParagraph>? lineToParagraph = null;
        if (history.Previous is not null)
        {
            lineToParagraph = new Dictionary<TextLine, TextParagraph>();
            foreach (var prevParagraph in history.Previous.AllParagraphs)
            {
                foreach (var line in prevParagraph.TextLines)
                    lineToParagraph[line] = prevParagraph;
            }
        }

        foreach (var paragraph in paragraphs)
        {
            TextParagraph? matched = null;
            var ok = lineToParagraph is not null && paragraph.TextLines.Count > 0;
            if (ok)
            {
                foreach (var line in paragraph.TextLines)
                {
                    if (line.PreviousFrameLineList.Count == 0)
                    {
                        ok = false;
                        break;
                    }

                    foreach (var prevLine in line.PreviousFrameLineList)
                    {
                        if (!lineToParagraph!.TryGetValue(prevLine, out var prevParagraph))
                        {
                            ok = false;
                            break;
                        }

                        if (matched is null)
                            matched = prevParagraph;
                        else if (!ReferenceEquals(matched, prevParagraph))
                        {
                            ok = false;
                            break;
                        }
                    }

                    if (!ok)
                        break;
                }
            }

            if (ok && matched is not null)
            {
                paragraph.Id = matched.Id;
                paragraph.HasPreviousFrameMatch = true;
                paragraph.PreviousFrameMatchAge = matched.PreviousFrameMatchAge + 1;
                paragraph.PreviousFrameText = matched.Text;
            }
            else
            {
                paragraph.HasPreviousFrameMatch = false;
                paragraph.PreviousFrameMatchAge = 1;
                paragraph.PreviousFrameText = string.Empty;
            }
        }
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

    /// <summary>
    /// True when <paramref name="lower"/> sits just below <paramref name="upper"/> along the paragraph normal.
    /// </summary>
    private bool CanMergeOrdered(TextLine upper, TextLine lower, double scale)
    {
        var avgHeight = Math.Max(1.0, (upper.Bounds.TextHeight + lower.Bounds.TextHeight) / 2.0);

        if (AngleDeltaDegrees(upper.Bounds.AngleDegrees, lower.Bounds.AngleDegrees)
            > _options.AngleToleranceDegrees)
            return false;

        var normal = upper.Bounds.Normal;
        var upperCenter = (upper.Bounds.P7 + upper.Bounds.P8) * 0.5f;
        var lowerCenter = (lower.Bounds.P7 + lower.Bounds.P8) * 0.5f;
        var spacing = Vector2.Dot(lowerCenter - upperCenter, normal);

        if (spacing < -0.25 * avgHeight)
            return false;

        var maxSpacing = _options.LineSpacingThreshold * avgHeight * scale;
        if (spacing > maxSpacing + 0.5)
            return false;

        var heightDiff = Math.Abs(lower.Bounds.TextHeight - upper.Bounds.TextHeight);
        var maxHeightDiff = _options.FontSizeTolerance * avgHeight * scale;
        if (heightDiff > maxHeightDiff)
            return false;

        return HasAlongOverlap(upper, lower, scale);
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
