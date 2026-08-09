using System.Numerics;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Links current lines to previous-frame lines, optionally snaps rails, and assigns stable ids/ages.
/// </summary>
internal sealed class LinePreviousMatcher
{
    private readonly ProximityTextLayoutOptions _options;
    private readonly LineRailSnapper _snapper;

    public LinePreviousMatcher(ProximityTextLayoutOptions options)
    {
        _options = options;
        _snapper = new LineRailSnapper(options);
    }

    public void Match(List<TextLine> lines, TextLayoutHistoryService history, bool snap)
    {
        if (history.Previous is null)
            return;

        var previousLines = history.Previous.Lines.OfType<TextLine>().ToList();
        if (previousLines.Count == 0)
            return;

        // Soft claims: current → best previous by word vote, then require start or end still anchored.
        var claims = new Dictionary<TextLine, (TextLine Prev, double Score)>();
        foreach (var line in lines)
        {
            if (!TryVotePrevious(line, history, out var prev, out var score))
                continue;
            if (!HasAlongEndpointAnchor(line, prev))
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
                _snapper.SnapToPrevious(winner);
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
                    _snapper.SnapToPrevious(line);
            }
        }

        AssignLineIds(lines);
    }

    /// <summary>
    /// Reuses the leftmost matched previous-frame line id and increments match age;
    /// otherwise marks the line as new (age = 1).
    /// </summary>
    private static void AssignLineIds(List<TextLine> lines)
    {
        foreach (var line in lines)
        {
            if (line.PreviousFrameLineList.Count == 0)
            {
                line.HasPreviousFrameMatch = false;
                line.PreviousFrameMatchAge = 1;
                line.PreviousFrameText = string.Empty;
                continue;
            }

            var prev = line.PreviousFrameLineList[0];
            line.Id = prev.Id;
            line.HasPreviousFrameMatch = true;
            line.PreviousFrameMatchAge = prev.PreviousFrameMatchAge + 1;
            line.PreviousFrameText = string.Join(" ", line.PreviousFrameLineList.Select(p => p.Text));
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

    /// <summary>
    /// True when the previous line's start or end still lines up with the current line's
    /// corresponding end within <see cref="ProximityTextLayoutOptions.CenterThresholdXFraction"/>
    /// (same along-tolerance used for previous-line word search).
    /// Rejects rigid sideways scroll where both ends have drifted.
    /// </summary>
    private bool HasAlongEndpointAnchor(TextLine current, TextLine previous)
    {
        var dir = previous.Bounds.Direction;
        var height = Math.Max(1.0, Math.Max(current.Bounds.TextHeight, previous.Bounds.TextHeight));
        var tol = _options.CenterThresholdXFraction * height;

        var prevStart = Vector2.Dot(previous.Bounds.P5, dir);
        var prevEnd = Vector2.Dot(previous.Bounds.P6, dir);
        var currStart = Vector2.Dot(current.Bounds.P5, dir);
        var currEnd = Vector2.Dot(current.Bounds.P6, dir);

        // Compare leading→trailing along reading direction (not raw P5/P6 order).
        if (prevStart > prevEnd) (prevStart, prevEnd) = (prevEnd, prevStart);
        if (currStart > currEnd) (currStart, currEnd) = (currEnd, currStart);

        var startDrift = Math.Abs(currStart - prevStart);
        var endDrift = Math.Abs(currEnd - prevEnd);
        return startDrift <= tol || endDrift <= tol;
    }

    private static bool CanAbsorbPrevious(TextLine current, List<TextLine> already, TextLine candidate)
    {
        var seed = already[0];
        if (!LineGeometry.AreAdjacentPreviousLines(seed, candidate)
            && !already.Any(a => LineGeometry.AreAdjacentPreviousLines(a, candidate)))
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
}
