using Zaya.OCR.Impl.ProximityTextLayout.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Links current lines to previous-frame lines.
/// </summary>
internal interface ILinePreviousMatcher
{
    void Match(List<TextLine> lines, ITextLayoutHistoryService history, bool snap);
}
