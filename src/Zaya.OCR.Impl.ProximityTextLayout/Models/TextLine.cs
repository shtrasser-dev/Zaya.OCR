using System.Drawing;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>
/// Default implementation of <see cref="ITextLine"/> for ProximityTextLayout.
/// </summary>
public sealed class TextLine : ITextLine
{
    /// <inheritdoc />
    public string Text { get; }

    /// <inheritdoc />
    public IReadOnlyList<IOCRWord> Words { get; }

    /// <inheritdoc />
    public Rectangle Bounds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextLine"/> class.
    /// </summary>
    /// <param name="text">The concatenated text of all words in this line.</param>
    /// <param name="words">The original recognized words that belong to this line.</param>
    /// <param name="bounds">The bounding rectangle that encompasses all words.</param>
    public TextLine(string text, IReadOnlyList<IOCRWord> words, Rectangle bounds)
    {
        Text = text;
        Words = words;
        Bounds = bounds;
    }
}
