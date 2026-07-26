using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>
/// Default implementation of <see cref="ITextResult"/> for ProximityTextLayout.
/// </summary>
public sealed class TextResult : ITextResult
{
    /// <inheritdoc />
    public IReadOnlyList<ITextParagraph> Paragraphs { get; }

    /// <inheritdoc />
    public string FullText { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextResult"/> class.
    /// </summary>
    /// <param name="paragraphs">The ordered list of paragraphs.</param>
    public TextResult(IReadOnlyList<ITextParagraph> paragraphs)
    {
        Paragraphs = paragraphs;
        FullText = string.Join("\n\n", paragraphs.Select(p => p.Text));
    }
}
