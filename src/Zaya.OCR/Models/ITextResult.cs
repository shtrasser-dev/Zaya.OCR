namespace Zaya.OCR.Models;

/// <summary>
/// Result of text layout: words, lines, and paragraphs structured from OCR.
/// </summary>
public interface ITextResult
{
    /// <summary>Words used for layout (after word filters).</summary>
    IReadOnlyList<ITextWord> Words { get; }

    /// <summary>Assembled lines.</summary>
    IReadOnlyList<ITextLine> Lines { get; }

    /// <summary>Emitted / stable paragraphs (alias of the primary show list).</summary>
    IReadOnlyList<ITextParagraph> Paragraphs { get; }

    /// <summary>Full text of all paragraphs, separated by empty lines.</summary>
    string FullText { get; }
}
