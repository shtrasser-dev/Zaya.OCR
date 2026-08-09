using System.Numerics;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>Shared line/word geometry helpers for assembly and temporal matching.</summary>
internal static class LineGeometry
{
    public static bool AreAdjacentPreviousLines(TextLine a, TextLine b)
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

    public static BoundingBox CreateLineBounds(IReadOnlyList<IOCRWord> lineWords)
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

    public static Vector2 AverageDirection(IReadOnlyList<IOCRWord> words)
    {
        var sum = Vector2.Zero;
        foreach (var word in words)
            sum += word.Bounds.Direction;
        return sum.LengthSquared() < 1e-12f ? Vector2.UnitX : Vector2.Normalize(sum);
    }

    public static float AverageAngleDegrees(IReadOnlyList<IOCRWord> words)
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

    public static double AngleDeltaDegrees(double a, double b)
    {
        var d = Math.Abs(a - b) % 360.0;
        if (d > 180.0)
            d = 360.0 - d;
        return d;
    }

    public static Vector2 ProjectOntoBaseline(Vector2 point, TextLine prev, Vector2 dir)
    {
        var mid = (prev.Bounds.P7 + prev.Bounds.P8) * 0.5f;
        var along = Vector2.Dot(point - mid, dir);
        return mid + dir * along;
    }

    public static void ApplySnapBounds(
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
}
