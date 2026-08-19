using Windows.Foundation;
using Zaya.OCR.Impl.WindowsMediaOcr.Services.Impl;
using Zaya.Primitives;

namespace Zaya.OCR.Tests;

public sealed class WindowsMediaOcrBoundsTests
{
    [Fact]
    public void FromWordRect_NoAngle_ReturnsAxisAlignedBox()
    {
        var box = WindowsMediaOcrBounds.FromWordRect(
            new Rect(10, 20, 40, 16),
            textAngleDegrees: null,
            imageWidth: 200,
            imageHeight: 100);

        Assert.Equal(BoundingBox.FromAxisAligned(10, 20, 40, 16), box);
        Assert.Equal(0, box.AngleDegrees, precision: 3);
    }

    [Fact]
    public void FromWordRect_ZeroAngle_ReturnsAxisAlignedBox()
    {
        var box = WindowsMediaOcrBounds.FromWordRect(
            new Rect(10, 20, 40, 16),
            textAngleDegrees: 0,
            imageWidth: 200,
            imageHeight: 100);

        Assert.Equal(BoundingBox.FromAxisAligned(10, 20, 40, 16), box);
    }

    [Fact]
    public void FromWordRect_WithAngle_RotatesCornersAroundImageCenter()
    {
        // Image 200x100, center (100,50). Word at (90,40)-(110,60) — centered box.
        var box = WindowsMediaOcrBounds.FromWordRect(
            new Rect(90, 40, 20, 20),
            textAngleDegrees: 90,
            imageWidth: 200,
            imageHeight: 100);

        // Clockwise 90° around center: (90,40) → (90,60), (110,40) → (90,40),
        // (110,60) → (110,40), (90,60) → (110,60).
        Assert.Equal(90f, WindowsMediaOcrBounds.RotateClockwise(new(90, 40), 100, 50, 90).X, precision: 3);
        Assert.Equal(60f, WindowsMediaOcrBounds.RotateClockwise(new(90, 40), 100, 50, 90).Y, precision: 3);

        Assert.InRange(Math.Abs(box.AngleDegrees), 80, 100);
        Assert.False(box.P1 == box.P2);
    }

    [Fact]
    public void RotateClockwise_90Degrees_MapsPointAsExpected()
    {
        var rotated = WindowsMediaOcrBounds.RotateClockwise(new(110, 50), cx: 100, cy: 50, degrees: 90);
        Assert.Equal(100f, rotated.X, precision: 3);
        Assert.Equal(40f, rotated.Y, precision: 3);
    }
}
