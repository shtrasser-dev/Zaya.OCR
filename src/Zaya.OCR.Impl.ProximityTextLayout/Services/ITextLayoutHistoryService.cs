using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.Primitives;
using Zaya.Primitives.OCR;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Narrow history facade for layout hysteresis (previous frozen frame).
/// </summary>
internal interface ITextLayoutHistoryService
{
    /// <summary>Last frozen frame, if any.</summary>
    TextResult? Previous { get; }

    /// <summary>Stores a frozen frame as previous.</summary>
    void Push(TextResult frozenFrame);

    /// <summary>Clears history.</summary>
    void Clear();

    /// <summary>Finds the previous-frame line whose band best covers <paramref name="bounds"/>.</summary>
    TextLine? FindPreviousLineCovering(BoundingBox bounds);

    /// <summary>True when both boxes map to the same previous line.</summary>
    bool WereOnSamePreviousLine(BoundingBox a, BoundingBox b);
}
