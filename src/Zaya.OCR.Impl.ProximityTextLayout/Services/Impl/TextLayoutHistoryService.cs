using System.Numerics;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.Primitives;
using Zaya.Primitives.OCR;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services.Impl;

/// <summary>
/// Narrow history facade for layout hysteresis (previous frozen frame).
/// </summary>
internal sealed class TextLayoutHistoryService : ITextLayoutHistoryService
{
    private readonly double _angleToleranceDegrees;
    private readonly double _alongTolFraction;
    private readonly double _acrossTolFraction;
    private TextResult? _previous;

    public TextLayoutHistoryService(
        double angleToleranceDegrees = 10,
        double alongTolFraction = 3.0,
        double acrossTolFraction = 0.75)
    {
        _angleToleranceDegrees = Math.Max(0, angleToleranceDegrees);
        _alongTolFraction = Math.Max(0, alongTolFraction);
        _acrossTolFraction = Math.Max(0, acrossTolFraction);
    }

    /// <summary>Last frozen frame, if any.</summary>
    public TextResult? Previous => _previous;

    /// <summary>Stores a frozen frame as previous.</summary>
    public void Push(TextResult frozenFrame)
    {
        if (!frozenFrame.IsFrozen)
            throw new ArgumentException("Only frozen frames may be pushed.", nameof(frozenFrame));

        _previous = frozenFrame;
    }

    /// <summary>Clears history.</summary>
    public void Clear() => _previous = null;

    /// <summary>Finds the previous-frame line whose band best covers <paramref name="bounds"/>.</summary>
    public TextLine? FindPreviousLineCovering(BoundingBox bounds)
        => _previous is null ? null : FindBestLine(_previous.AssembledLines, bounds);

    /// <summary>True when both boxes map to the same previous line.</summary>
    public bool WereOnSamePreviousLine(BoundingBox a, BoundingBox b)
    {
        var la = FindPreviousLineCovering(a);
        var lb = FindPreviousLineCovering(b);
        return la is not null && ReferenceEquals(la, lb);
    }

    private TextLine? FindBestLine(IEnumerable<TextLine>? lines, BoundingBox query)
    {
        if (lines is null)
            return null;

        TextLine? best = null;
        var bestScore = double.MaxValue;
        var q = (query.P5 + query.P6) * 0.5f;

        foreach (var line in lines)
        {
            if (AngleDeltaDegrees(query.AngleDegrees, line.Bounds.AngleDegrees) > _angleToleranceDegrees)
                continue;
            if (!TryScoreCovering(line, q, out var score))
                continue;
            if (score >= bestScore)
                continue;
            bestScore = score;
            best = line;
        }

        return best;
    }

    /// <summary>Score how well a point sits in a line band (lower is better).</summary>
    internal bool TryScoreCovering(TextLine line, Vector2 point, out double score)
    {
        score = double.MaxValue;
        var dir = line.Bounds.Direction;
        var normal = line.Bounds.Normal;
        var height = Math.Max(1.0, line.Bounds.TextHeight);
        var acrossTol = _acrossTolFraction * height;

        var origin = (line.Bounds.P5 + line.Bounds.P6) * 0.5f;
        var delta = point - origin;
        var across = Math.Abs(Vector2.Dot(delta, normal));
        if (across > acrossTol)
            return false;

        var along = Vector2.Dot(point, dir);
        var a0 = Vector2.Dot(line.Bounds.P5, dir);
        var a1 = Vector2.Dot(line.Bounds.P6, dir);
        if (a0 > a1)
            (a0, a1) = (a1, a0);

        // Distance to the along-interval (0 if inside). Growing OCR often extends past previous P6.
        var alongDist = along < a0 ? a0 - along : along > a1 ? along - a1 : 0;
        var alongTol = _alongTolFraction * height;
        if (alongDist > alongTol)
            return false;

        score = across + 0.05 * alongDist;
        return true;
    }

    private static double AngleDeltaDegrees(double a, double b)
    {
        var d = Math.Abs(a - b) % 360.0;
        if (d > 180.0)
            d = 360.0 - d;
        return d;
    }
}
