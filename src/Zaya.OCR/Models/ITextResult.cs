namespace Zaya.OCR.Models;

/// <summary>
/// Represents the result of text layout processing: structured paragraphs
/// parsed from individual OCR words.
/// </summary>
public interface ITextResult
{
    /// <summary>
    /// Gets the ordered list of paragraphs.
    /// </summary>
    IReadOnlyList<ITextParagraph> Paragraphs { get; }

    /// <summary>
    /// Gets the full text of all paragraphs, separated by empty lines.
    /// </summary>
    string FullText { get; }
}
