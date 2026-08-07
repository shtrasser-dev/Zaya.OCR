using System.Numerics;
using Windows.Foundation;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.WindowsMediaOcr;

/// <summary>
/// Maps WinRT <see cref="OcrWord"/> rectangles onto <see cref="BoundingBox"/>,
/// applying <c>OcrResult.TextAngle</c> when present.
/// </summary>
internal static class WindowsMediaOcrBounds
{
    /// <summary>
    /// Builds an oriented box from an axis-aligned WinRT word rect.
    /// When <paramref name="textAngleDegrees"/> is set, corners are rotated clockwise
    /// around the image center (WinRT overlay convention).
    /// </summary>
    public static BoundingBox FromWordRect(
        Rect rect,
        double? textAngleDegrees,
        float imageWidth,
        float imageHeight)
    {
        var x = (float)rect.X;
        var y = (float)rect.Y;
        var w = Math.Max(1f, (float)rect.Width);
        var h = Math.Max(1f, (float)rect.Height);

        var p1 = new Vector2(x, y);
        var p2 = new Vector2(x + w, y);
        var p3 = new Vector2(x + w, y + h);
        var p4 = new Vector2(x, y + h);

        if (textAngleDegrees is null || Math.Abs(textAngleDegrees.Value) < 1e-4)
            return new BoundingBox(p1, p2, p3, p4);

        var cx = imageWidth * 0.5f;
        var cy = imageHeight * 0.5f;
        var angle = (float)textAngleDegrees.Value;
        return new BoundingBox(
            RotateClockwise(p1, cx, cy, angle),
            RotateClockwise(p2, cx, cy, angle),
            RotateClockwise(p3, cx, cy, angle),
            RotateClockwise(p4, cx, cy, angle));
    }

    /// <summary>
    /// Clockwise rotation around (<paramref name="cx"/>, <paramref name="cy"/>) in image space (Y down).
    /// </summary>
    public static Vector2 RotateClockwise(Vector2 point, float cx, float cy, float degrees)
    {
        var rad = degrees * (MathF.PI / 180f);
        var cos = MathF.Cos(rad);
        var sin = MathF.Sin(rad);
        var dx = point.X - cx;
        var dy = point.Y - cy;
        return new Vector2(
            cx + dx * cos + dy * sin,
            cy - dx * sin + dy * cos);
    }
}
