using System.Numerics;
using Zaya.OCR.Impl.ProximityTextLayout.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>Snaps current-line display rails to matched previous-frame lines.</summary>
internal sealed class LineRailSnapper
{
    private readonly ProximityTextLayoutOptions _options;

    public LineRailSnapper(ProximityTextLayoutOptions options)
    {
        _options = options;
    }

    public void SnapToPrevious(TextLine line)
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

        var prevStart = LineGeometry.ProjectOntoBaseline(left.Bounds.P5, left, dir);
        var prevEnd = LineGeometry.ProjectOntoBaseline(right.Bounds.P6, right, dir);

        // Exact text match (ignore case): freeze display rails to the previous union.
        var previousText = string.Join(" ", list.Select(p => p.Text));
        if (string.Equals(line.Text, previousText, StringComparison.OrdinalIgnoreCase))
        {
            LineGeometry.ApplySnapBounds(line, prevStart, prevEnd, dir, normal, half);
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
            snapEnd = LineGeometry.ProjectOntoBaseline(prevStart + dir * Math.Max(curEndAlong, prevLen), left, dir);
        }
        else if (endClose)
        {
            // Growing / shrinking at the leading edge; keep previous end.
            snapEnd = prevEnd;
            var curStartAlong = Vector2.Dot(newP5 - prevEnd, dir);
            var prevLen = Vector2.Dot(prevEnd - prevStart, dir);
            snapStart = LineGeometry.ProjectOntoBaseline(prevEnd + dir * Math.Min(curStartAlong, -prevLen), right, dir);
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

        LineGeometry.ApplySnapBounds(line, snapStart, snapEnd, dir, normal, half);
    }
}
