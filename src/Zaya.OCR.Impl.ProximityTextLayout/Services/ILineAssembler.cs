using Zaya.OCR.Impl.ProximityTextLayout.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Assembles words into lines and links them to the previous frame.
/// </summary>
internal interface ILineAssembler
{
    void Assemble(TextResult frame, ITextLayoutHistoryService history);
}
