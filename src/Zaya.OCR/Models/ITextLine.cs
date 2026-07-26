using System.Drawing;

namespace Zaya.OCR.Models;

/// <summary>
/// Represents a single line of text composed of one or more recognized words.
/// </summary>
public interface ITextLine
{
    /// <summary>
    /// Gets the concatenated text of all words in this line.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets the original recognized words that belong to this line.
    /// </summary>
    IReadOnlyList<IOCRWord> Words { get; }

    /// <summary>
    /// Gets the bounding rectangle that encompasses all words in this line.
    /// </summary>
    Rectangle Bounds { get; }
}
