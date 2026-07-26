using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>
/// Default implementation of <see cref="ITextParagraph"/> for ProximityTextLayout.
/// </summary>
public sealed class TextParagraph : ITextParagraph
{
    /// <inheritdoc />
    public string Text { get; }

    /// <inheritdoc />
    public IReadOnlyList<ITextLine> Lines { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextParagraph"/> class.
    /// </summary>
    /// <param name="text">The full text of this paragraph, with lines separated by newlines.</param>
    /// <param name="lines">The lines that make up this paragraph.</param>
    public TextParagraph(string text, IReadOnlyList<ITextLine> lines)
    {
        Text = text;
        Lines = lines;
    }
}
