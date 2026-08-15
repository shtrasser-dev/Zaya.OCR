using Zaya.OCR.Impl.ProximityTextLayout.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Appends previous-frame paragraphs that have no match (ghosts).
/// </summary>
internal interface IParagraphGhostService
{
    void AppendGhosts(TextResult frame, ITextLayoutHistoryService history);
}
