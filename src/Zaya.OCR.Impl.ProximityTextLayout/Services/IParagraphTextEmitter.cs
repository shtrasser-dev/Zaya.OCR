using Zaya.OCR.Impl.ProximityTextLayout.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Decides which paragraphs to emit and with which display text.
/// </summary>
internal interface IParagraphTextEmitter
{
    void Emit(TextResult frame, ITextLayoutHistoryService history);
}
