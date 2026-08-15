using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Pattern filters applied at word / line / paragraph stages of layout.
/// </summary>
internal interface ILayoutTextFilter
{
    bool IsEmpty { get; }

    IReadOnlyList<IOCRWord> FilterWords(IReadOnlyList<IOCRWord> words);

    IReadOnlyList<ITextLine> FilterLines(IReadOnlyList<ITextLine> lines);

    IReadOnlyList<ITextParagraph> FilterParagraphs(IReadOnlyList<ITextParagraph> paragraphs);
}
