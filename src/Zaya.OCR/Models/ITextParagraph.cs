namespace Zaya.OCR.Models;

/// <summary>
/// Represents a paragraph of text composed of one or more lines.
/// </summary>
public interface ITextParagraph
{
    /// <summary>
    /// Gets the full text of this paragraph, with lines separated by newlines.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets the lines that make up this paragraph.
    /// </summary>
    IReadOnlyList<ITextLine> Lines { get; }
}
