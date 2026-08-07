using System.Numerics;

namespace Zaya.OCR.Models;

/// <summary>
/// Oriented quadrilateral in image pixel space (Y down).
/// Corner order matches OneOCR: P1→P2 along the top (reading direction), P3→P4 along the bottom back.
/// </summary>
/// <remarks>
/// <para>
/// <c>P1 ----→ P2</c><br/>
/// <c>↑         ↓</c><br/>
/// <c>P4 ←---- P3</c>
/// </para>
/// <para>
/// <see cref="Direction"/> and <see cref="AngleDegrees"/> are derived from the average of
/// the top edge (P2−P1) and bottom edge (P3−P4).
/// </para>
/// </remarks>
public readonly struct BoundingBox : IEquatable<BoundingBox>
{
    /// <summary>Empty box (all corners at origin).</summary>
    public static BoundingBox Empty { get; }

    /// <summary>Start of the top edge (reading start).</summary>
    public Vector2 P1 { get; }

    /// <summary>End of the top edge (reading end).</summary>
    public Vector2 P2 { get; }

    /// <summary>End of the bottom edge.</summary>
    public Vector2 P3 { get; }

    /// <summary>Start of the bottom edge.</summary>
    public Vector2 P4 { get; }

    /// <summary>
    /// Unit vector along reading direction: normalize((P2−P1)+(P3−P4)).
    /// Falls back to (1,0) when both edges are degenerate.
    /// </summary>
    public Vector2 Direction { get; }

    /// <summary>
    /// Tilt of <see cref="Direction"/> from +X, in degrees (image space, Y down).
    /// </summary>
    public float AngleDegrees { get; }

    /// <summary>Minimum X of the axis-aligned bounds.</summary>
    public float MinX { get; }

    /// <summary>Minimum Y of the axis-aligned bounds.</summary>
    public float MinY { get; }

    /// <summary>Maximum X of the axis-aligned bounds.</summary>
    public float MaxX { get; }

    /// <summary>Maximum Y of the axis-aligned bounds.</summary>
    public float MaxY { get; }

    /// <summary>Integer left of the AABB (floor of <see cref="MinX"/>).</summary>
    public int X => (int)MathF.Floor(MinX);

    /// <summary>Integer top of the AABB (floor of <see cref="MinY"/>).</summary>
    public int Y => (int)MathF.Floor(MinY);

    /// <summary>Same as <see cref="X"/>.</summary>
    public int Left => X;

    /// <summary>Same as <see cref="Y"/>.</summary>
    public int Top => Y;

    /// <summary>Integer right of the AABB (ceil of <see cref="MaxX"/>).</summary>
    public int Right => (int)MathF.Ceiling(MaxX);

    /// <summary>Integer bottom of the AABB (ceil of <see cref="MaxY"/>).</summary>
    public int Bottom => (int)MathF.Ceiling(MaxY);

    /// <summary>Integer AABB width.</summary>
    public int Width => Math.Max(0, Right - Left);

    /// <summary>Integer AABB height.</summary>
    public int Height => Math.Max(0, Bottom - Top);

    /// <summary>True when all corners are at the origin and the box has no extent.</summary>
    public bool IsEmpty => Width == 0 && Height == 0
                           && P1 == Vector2.Zero && P2 == Vector2.Zero
                           && P3 == Vector2.Zero && P4 == Vector2.Zero;

    /// <summary>
    /// Midpoint of the leading edge (P1–P4): start of the word along reading direction.
    /// </summary>
    public Vector2 P5 => (P1 + P4) * 0.5f;

    /// <summary>
    /// Midpoint of the trailing edge (P2–P3): end of the word along reading direction.
    /// </summary>
    public Vector2 P6 => (P2 + P3) * 0.5f;

    /// <summary>
    /// Midpoint of the top edge (P1–P2).
    /// </summary>
    public Vector2 P7 => (P1 + P2) * 0.5f;

    /// <summary>
    /// Midpoint of the bottom edge (P4–P3).
    /// </summary>
    public Vector2 P8 => (P4 + P3) * 0.5f;

    /// <summary>
    /// Unit normal rotated 90° from <see cref="Direction"/> (points toward the bottom of the glyph in image space for LTR horizontal text).
    /// </summary>
    public Vector2 Normal => new(-Direction.Y, Direction.X);

    /// <summary>
    /// Glyph height along the normal (distance between top and bottom edge midpoints).
    /// </summary>
    public float TextHeight
    {
        get
        {
            var topMid = (P1 + P2) * 0.5f;
            var bottomMid = (P4 + P3) * 0.5f;
            return MathF.Max(1e-3f, Vector2.Distance(topMid, bottomMid));
        }
    }

    /// <summary>
    /// Creates a bounding box from four corners in OneOCR order.
    /// </summary>
    public BoundingBox(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
        P4 = p4;

        MinX = Min4(p1.X, p2.X, p3.X, p4.X);
        MinY = Min4(p1.Y, p2.Y, p3.Y, p4.Y);
        MaxX = Max4(p1.X, p2.X, p3.X, p4.X);
        MaxY = Max4(p1.Y, p2.Y, p3.Y, p4.Y);

        var top = p2 - p1;
        var bottom = p3 - p4;
        var sum = top + bottom;
        if (sum.LengthSquared() < 1e-12f)
        {
            if (top.LengthSquared() >= 1e-12f)
                sum = top;
            else if (bottom.LengthSquared() >= 1e-12f)
                sum = bottom;
            else
                sum = Vector2.UnitX;
        }

        Direction = Vector2.Normalize(sum);
        AngleDegrees = MathF.Atan2(Direction.Y, Direction.X) * (180f / MathF.PI);
    }

    /// <summary>
    /// Creates an axis-aligned box (P1 top-left → P2 top-right → P3 bottom-right → P4 bottom-left).
    /// </summary>
    public static BoundingBox FromAxisAligned(float x, float y, float width, float height)
    {
        var w = MathF.Max(0, width);
        var h = MathF.Max(0, height);
        return new BoundingBox(
            new Vector2(x, y),
            new Vector2(x + w, y),
            new Vector2(x + w, y + h),
            new Vector2(x, y + h));
    }

    /// <summary>
    /// Creates an axis-aligned box from integer pixel coordinates.
    /// </summary>
    public static BoundingBox FromAxisAligned(int x, int y, int width, int height)
        => FromAxisAligned((float)x, y, width, height);

    /// <summary>
    /// Creates an axis-aligned box from left/top/right/bottom edges.
    /// </summary>
    public static BoundingBox FromLTRB(int left, int top, int right, int bottom)
        => FromAxisAligned(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));

    /// <summary>
    /// Returns the integer axis-aligned rectangle (X, Y, Width, Height).
    /// </summary>
    public (int X, int Y, int Width, int Height) ToRect()
        => (X, Y, Width, Height);

    /// <inheritdoc />
    public bool Equals(BoundingBox other)
        => P1.Equals(other.P1)
           && P2.Equals(other.P2)
           && P3.Equals(other.P3)
           && P4.Equals(other.P4);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is BoundingBox other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(P1, P2, P3, P4);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(BoundingBox left, BoundingBox right)
        => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(BoundingBox left, BoundingBox right)
        => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString()
        => IsEmpty
            ? "BoundingBox.Empty"
            : $"BoundingBox(P1={P1}, P2={P2}, P3={P3}, P4={P4}, Angle={AngleDegrees:0.##}°)";

    private static float Min4(float a, float b, float c, float d)
        => MathF.Min(MathF.Min(a, b), MathF.Min(c, d));

    private static float Max4(float a, float b, float c, float d)
        => MathF.Max(MathF.Max(a, b), MathF.Max(c, d));
}
