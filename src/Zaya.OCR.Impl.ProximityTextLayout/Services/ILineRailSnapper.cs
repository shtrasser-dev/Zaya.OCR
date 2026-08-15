using Zaya.OCR.Impl.ProximityTextLayout.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Snaps current-line display rails to matched previous-frame lines.
/// </summary>
internal interface ILineRailSnapper
{
    void SnapToPrevious(TextLine line);
}
