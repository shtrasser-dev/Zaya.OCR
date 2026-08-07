using System.Numerics;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Assembles words into lines, links PreviousFrameLineList, and snaps display rails.
/// </summary>
internal sealed class LineAssembler
{
    private readonly ProximityTextLayoutOptions _options;
    private readonly double _lineMergeHysteresis;
    private readonly double _sameLineWordGapHysteresis;

    public LineAssembler(ProximityTextLayoutOptions options)
    {
        _options = options;
        _lineMergeHysteresis = Math.Clamp(options.ParagraphMergeHysteresis, 1.0, 3.0);
        _sameLineWordGapHysteresis = Math.Clamp(options.SameLineWordGapHysteresis, 1.0, 20.0);
    }

    public void Assemble(TextResult frame, TextLayoutHistoryService history)
    {
        var words = frame.MutableWords.Cast<IOCRWord>().ToList();
        if (words.Count == 0)
        {
            frame.MutableLines = [];
            return;
        }

        var lines = BuildLines(words, history);
        MatchPrevious(lines, history, snap: _options.EnableStabilization);
        frame.MutableLines = lines;
    }

    private List<TextLine> BuildLines(List<IOCRWord> words, TextLayoutHistoryService history)
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

            var sortDir = AverageDirection(lineWords);
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

        lines.Sort((a, b) =>
        {
            var ay = (a.Bounds.MinY + a.Bounds.MaxY) * 0.5f;
            var by = (b.Bounds.MinY + b.Bounds.MaxY) * 0.5f;
            var cmp = ay.CompareTo(by);
            return cmp != 0 ? cmp : a.Bounds.MinX.CompareTo(b.Bounds.MinX);
        });

        return lines;
    }

    private bool TryAppendWord(List<IOCRWord> lineWords, List<IOCRWord> remaining, TextLayoutHistoryService history)
    {
        var last = lineWords[^1];
        var bestIndex = FindBestNeighborIndex(lineWords, remaining, last.Bounds.P6, append: true, history);
        if (bestIndex < 0)
            return false;

        lineWords.Add(remaining[bestIndex]);
        remaining.RemoveAt(bestIndex);
        return true;
    }

    private bool TryPrependWord(List<IOCRWord> lineWords, List<IOCRWord> remaining, TextLayoutHistoryService history)
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
        TextLayoutHistoryService history)
    {
        var refWord = append ? lineWords[^1] : lineWords[0];
        var dir = refWord.Bounds.Direction;
        var normal = refWord.Bounds.Normal;
        var height = Math.Max(1.0, refWord.Bounds.TextHeight);
        var lineAngle = AverageAngleDegrees(lineWords);

        var bestIndex = -1;
        var bestScore = double.MaxValue;

        for (var i = 0; i < remaining.Count; i++)
        {
            var candidate = remaining[i];
            if (AngleDeltaDegrees(lineAngle, candidate.Bounds.AngleDegrees) > _options.AngleToleranceDegrees)
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
                    && AreAdjacentPreviousLines(prevA, prevB))
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

    private static bool AreAdjacentPreviousLines(TextLine a, TextLine b)
    {
        if (ReferenceEquals(a.NextLine, b) || ReferenceEquals(a.PrevLine, b)
            || ReferenceEquals(b.NextLine, a) || ReferenceEquals(b.PrevLine, a))
            return true;

        var dir = a.Bounds.Direction;
        var normal = a.Bounds.Normal;
        var height = Math.Max(1f, (a.Bounds.TextHeight + b.Bounds.TextHeight) * 0.5f);
        var ca = (a.Bounds.P5 + a.Bounds.P6) * 0.5f;
        var cb = (b.Bounds.P5 + b.Bounds.P6) * 0.5f;
        if (Math.Abs(Vector2.Dot(cb - ca, normal)) > 0.75f * height)
            return false;

        var a0 = Vector2.Dot(a.Bounds.P5, dir);
        var a1 = Vector2.Dot(a.Bounds.P6, dir);
        var b0 = Vector2.Dot(b.Bounds.P5, dir);
        var b1 = Vector2.Dot(b.Bounds.P6, dir);
        if (a0 > a1) (a0, a1) = (a1, a0);
        if (b0 > b1) (b0, b1) = (b1, b0);
        var gap = b0 > a1 ? b0 - a1 : a0 > b1 ? a0 - b1 : 0;
        return gap <= 2.5f * height;
    }

    private void MatchPrevious(List<TextLine> lines, TextLayoutHistoryService history, bool snap)
    {
        if (history.Previous is null)
            return;

        var previousLines = history.Previous.Lines.OfType<TextLine>().ToList();
        if (previousLines.Count == 0)
            return;

        // Soft claims: current → best previous by word vote.
        var claims = new Dictionary<TextLine, (TextLine Prev, double Score)>();
        foreach (var line in lines)
        {
            if (!TryVotePrevious(line, history, out var prev, out var score))
                continue;
            claims[line] = (prev, score);
        }

        // N current → 1 prev: leftmost along reading wins.
        var claimedPrev = new HashSet<TextLine>();
        foreach (var group in claims.GroupBy(kv => kv.Value.Prev))
        {
            var winners = group
                .OrderBy(kv => Vector2.Dot((kv.Key.Bounds.P5 + kv.Key.Bounds.P6) * 0.5f, group.Key.Bounds.Direction))
                .ToList();
            var winner = winners[0].Key;
            claimedPrev.Add(group.Key);
            winner.SetPreviousFrameLines([group.Key], winner.Bounds.Direction);
            if (snap)
                SnapToPrevious(winner);
        }

        // 1 current → N prev: absorb adjacent unclaimed previous lines covered by current.
        foreach (var line in lines)
        {
            if (line.PreviousFrameLineList.Count == 0)
                continue;

            var dir = line.Bounds.Direction;
            var absorbed = new List<TextLine>(line.PreviousFrameLineList);
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var prev in previousLines)
                {
                    if (claimedPrev.Contains(prev) || absorbed.Contains(prev))
                        continue;
                    if (!CanAbsorbPrevious(line, absorbed, prev))
                        continue;
                    absorbed.Add(prev);
                    claimedPrev.Add(prev);
                    changed = true;
                }
            }

            if (absorbed.Count > line.PreviousFrameLineList.Count)
            {
                line.SetPreviousFrameLines(absorbed, dir);
                if (snap)
                    SnapToPrevious(line);
            }
        }
    }

    private static bool TryVotePrevious(
        TextLine line,
        TextLayoutHistoryService history,
        out TextLine prev,
        out double score)
    {
        prev = null!;
        score = 0;
        var votes = new Dictionary<TextLine, int>();
        foreach (var word in line.Words)
        {
            var hit = history.FindPreviousLineCovering(word.Bounds);
            if (hit is null)
                continue;
            votes[hit] = votes.GetValueOrDefault(hit) + 1;
        }

        if (votes.Count == 0)
        {
            var hit = FindByMidBody(line, history);
            if (hit is null)
                return false;
            prev = hit;
            score = 0.5;
            return true;
        }

        var best = votes.OrderByDescending(kv => kv.Value).First();
        if (best.Value < Math.Max(1, (line.Words.Count + 1) / 2))
            return false;

        prev = best.Key;
        score = best.Value;
        return true;
    }

    private static TextLine? FindByMidBody(TextLine line, TextLayoutHistoryService history)
    {
        var dir = line.Bounds.Direction;
        var a0 = Vector2.Dot(line.Bounds.P5, dir);
        var a1 = Vector2.Dot(line.Bounds.P6, dir);
        if (a0 > a1) (a0, a1) = (a1, a0);
        var t0 = a0 + 0.2f * (a1 - a0);
        var t1 = a0 + 0.8f * (a1 - a0);
        var midAlong = 0.5f * (t0 + t1);
        var origin = (line.Bounds.P7 + line.Bounds.P8) * 0.5f;
        var originAlong = Vector2.Dot(origin, dir);
        var point = origin + dir * (midAlong - originAlong);
        var half = Math.Max(1f, line.Bounds.TextHeight * 0.25f);
        var box = new BoundingBox(
            point - dir * half - line.Bounds.Normal * half,
            point + dir * half - line.Bounds.Normal * half,
            point + dir * half + line.Bounds.Normal * half,
            point - dir * half + line.Bounds.Normal * half);
        return history.FindPreviousLineCovering(box);
    }

    private bool CanAbsorbPrevious(TextLine current, List<TextLine> already, TextLine candidate)
    {
        var seed = already[0];
        if (!AreAdjacentPreviousLines(seed, candidate)
            && !already.Any(a => AreAdjacentPreviousLines(a, candidate)))
            return false;

        var dir = current.Bounds.Direction;
        var normal = current.Bounds.Normal;
        var height = Math.Max(1f, current.Bounds.TextHeight);
        var cc = (current.Bounds.P7 + current.Bounds.P8) * 0.5f;
        var pc = (candidate.Bounds.P7 + candidate.Bounds.P8) * 0.5f;
        if (Math.Abs(Vector2.Dot(pc - cc, normal)) > 0.75f * height)
            return false;

        // Current along-interval should cover most of candidate.
        var c0 = Vector2.Dot(current.Bounds.P5, dir);
        var c1 = Vector2.Dot(current.Bounds.P6, dir);
        var p0 = Vector2.Dot(candidate.Bounds.P5, dir);
        var p1 = Vector2.Dot(candidate.Bounds.P6, dir);
        if (c0 > c1) (c0, c1) = (c1, c0);
        if (p0 > p1) (p0, p1) = (p1, p0);
        var overlap = Math.Max(0, Math.Min(c1, p1) - Math.Max(c0, p0));
        var plen = Math.Max(1e-3f, p1 - p0);
        return overlap / plen >= 0.5f;
    }

    private void SnapToPrevious(TextLine line)
    {
        var list = line.PreviousFrameLineList;
        if (list.Count == 0)
            return;

        var dir = list[0].Bounds.Direction;
        line.SortPreviousFrameLineList(dir);
        var left = list[0];
        var right = list[^1];
        var height = Math.Max(left.Bounds.TextHeight, right.Bounds.TextHeight);
        var half = Math.Max(0.5f, height * 0.5f);
        var normal = new Vector2(-dir.Y, dir.X);

        var prevStart = ProjectOntoBaseline(left.Bounds.P5, left, dir);
        var prevEnd = ProjectOntoBaseline(right.Bounds.P6, right, dir);

        // Exact text match (ignore case): freeze display rails to the previous union.
        var previousText = string.Join(" ", list.Select(p => p.Text));
        if (string.Equals(line.Text, previousText, StringComparison.OrdinalIgnoreCase))
        {
            ApplySnapBounds(line, prevStart, prevEnd, dir, normal, half);
            return;
        }

        var newP5 = line.Bounds.P5;
        var newP6 = line.Bounds.P6;
        var startTol = _options.WordGapThreshold * height;
        var endTol = startTol;

        var startDelta = Vector2.Dot(newP5 - left.Bounds.P5, dir);
        var endDelta = Vector2.Dot(newP6 - right.Bounds.P6, dir);
        var startClose = Math.Abs(startDelta) <= startTol;
        var endClose = Math.Abs(endDelta) <= endTol;

        Vector2 snapStart;
        Vector2 snapEnd;
        if (startClose && endClose)
        {
            // Small jitter on both ends: lock the full previous segment.
            snapStart = prevStart;
            snapEnd = prevEnd;
        }
        else if (startClose)
        {
            // Growing / shrinking at the trailing edge; keep previous start.
            snapStart = prevStart;
            var curEndAlong = Vector2.Dot(newP6 - prevStart, dir);
            var prevLen = Vector2.Dot(prevEnd - prevStart, dir);
            snapEnd = ProjectOntoBaseline(prevStart + dir * Math.Max(curEndAlong, prevLen), left, dir);
        }
        else if (endClose)
        {
            // Growing / shrinking at the leading edge; keep previous end.
            snapEnd = prevEnd;
            var curStartAlong = Vector2.Dot(newP5 - prevEnd, dir);
            var prevLen = Vector2.Dot(prevEnd - prevStart, dir);
            snapStart = ProjectOntoBaseline(prevEnd + dir * Math.Min(curStartAlong, -prevLen), right, dir);
        }
        else
        {
            // Far from both ends: only kill across-track drift.
            var midPrev = (left.Bounds.P7 + left.Bounds.P8) * 0.5f;
            var curMid = (line.Bounds.P7 + line.Bounds.P8) * 0.5f;
            var shift = Vector2.Dot(midPrev - curMid, normal) * normal;
            snapStart = newP5 + shift;
            snapEnd = newP6 + shift;
        }

        ApplySnapBounds(line, snapStart, snapEnd, dir, normal, half);
    }

    private static void ApplySnapBounds(
        TextLine line,
        Vector2 snapStart,
        Vector2 snapEnd,
        Vector2 dir,
        Vector2 normal,
        float half)
    {
        // Ensure start→end follows reading direction.
        if (Vector2.Dot(snapEnd - snapStart, dir) < 0)
            (snapStart, snapEnd) = (snapEnd, snapStart);

        line.Bounds = new BoundingBox(
            snapStart - normal * half,
            snapEnd - normal * half,
            snapEnd + normal * half,
            snapStart + normal * half);
    }

    private static Vector2 ProjectOntoBaseline(Vector2 point, TextLine prev, Vector2 dir)
    {
        var mid = (prev.Bounds.P7 + prev.Bounds.P8) * 0.5f;
        var along = Vector2.Dot(point - mid, dir);
        return mid + dir * along;
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
        var bounds = CreateLineBounds(lineWords);
        return new TextLine(text, lineWords.ToList(), bounds);
    }

    internal static BoundingBox CreateLineBounds(IReadOnlyList<IOCRWord> lineWords)
    {
        if (lineWords.Count == 0)
            return BoundingBox.Empty;

        var dir = AverageDirection(lineWords);
        var normal = new Vector2(-dir.Y, dir.X);
        var start = lineWords[0].Bounds.P5;
        var end = lineWords[^1].Bounds.P6;
        if (Vector2.Dot(end - start, dir) < 0)
            (start, end) = (end, start);

        var halfHeight = MathF.Max(0.5f, lineWords.Max(w => w.Bounds.TextHeight) * 0.5f);

        var startProj = float.PositiveInfinity;
        var endProj = float.NegativeInfinity;
        foreach (var word in lineWords)
        {
            foreach (var pt in new[] { word.Bounds.P5, word.Bounds.P6 })
            {
                var proj = Vector2.Dot(pt, dir);
                if (proj < startProj) startProj = proj;
                if (proj > endProj) endProj = proj;
            }
        }

        var origin = (start + end) * 0.5f;
        var originAlong = Vector2.Dot(origin, dir);
        start = origin + dir * (startProj - originAlong);
        end = origin + dir * (endProj - originAlong);

        return new BoundingBox(
            start - normal * halfHeight,
            end - normal * halfHeight,
            end + normal * halfHeight,
            start + normal * halfHeight);
    }

    private static Vector2 AverageDirection(IReadOnlyList<IOCRWord> words)
    {
        var sum = Vector2.Zero;
        foreach (var word in words)
            sum += word.Bounds.Direction;
        return sum.LengthSquared() < 1e-12f ? Vector2.UnitX : Vector2.Normalize(sum);
    }

    private static float AverageAngleDegrees(IReadOnlyList<IOCRWord> words)
    {
        if (words.Count == 0)
            return 0;

        var sum = Vector2.Zero;
        foreach (var word in words)
        {
            var rad = word.Bounds.AngleDegrees * (MathF.PI / 180f);
            sum += new Vector2(MathF.Cos(rad), MathF.Sin(rad));
        }

        if (sum.LengthSquared() < 1e-12f)
            return words[0].Bounds.AngleDegrees;

        return MathF.Atan2(sum.Y, sum.X) * (180f / MathF.PI);
    }

    private static double AngleDeltaDegrees(double a, double b)
    {
        var d = Math.Abs(a - b) % 360.0;
        if (d > 180.0)
            d = 360.0 - d;
        return d;
    }
}
