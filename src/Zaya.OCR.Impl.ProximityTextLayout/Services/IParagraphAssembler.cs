using Zaya.OCR.Impl.ProximityTextLayout.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Groups lines into paragraphs.
/// </summary>
internal interface IParagraphAssembler
{
    void Assemble(TextResult frame, ITextLayoutHistoryService history);
}
