using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>Internal paragraph with emit/ghost metadata.</summary>
public sealed class TextParagraph : ITextParagraph
{
    /// <inheritdoc />
    public string Text { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<ITextLine> Lines { get; }

    /// <summary>Concrete lines for pipeline use.</summary>
    public IReadOnlyList<TextLine> TextLines { get; }

    /// <summary>
    /// OCR/layout text at paragraph creation (never rewritten by emit/snap display choices).
    /// </summary>
    public string OriginalText { get; }

    /// <summary>Normalized compare key (optional cache).</summary>
    public string? CompareKey { get; set; }

    /// <summary>True when included in the Stable emit list this frame.</summary>
    public bool IsEmitted { get; set; }

    /// <summary>
    /// True when this paragraph was shown to the user (emitted or ghosted as visible).
    /// Persists in history for <c>HoldNewBlocks</c> decisions on the next frame.
    /// </summary>
    public bool WasShown { get; set; }

    /// <summary>True when carried forward as a ghost.</summary>
    public bool IsGhost { get; set; }

    /// <summary>Consecutive ghost frames (0 = live emit).</summary>
    public int GhostAge { get; set; }

    /// <summary>Creates a paragraph from lines.</summary>
    public TextParagraph(string text, IReadOnlyList<TextLine> lines)
    {
        Text = text;
        OriginalText = text;
        TextLines = lines;
        Lines = lines;
    }

    /// <summary>Creates a paragraph from interface lines (filters / tests).</summary>
    public TextParagraph(string text, IReadOnlyList<ITextLine> lines)
    {
        Text = text;
        OriginalText = text;
        var concrete = lines.OfType<TextLine>().ToList();
        if (concrete.Count != lines.Count)
            concrete = lines.Select(l => l as TextLine ?? new TextLine(l.Text, l.Words, l.Bounds)).ToList();
        TextLines = concrete;
        Lines = concrete;
    }

    /// <summary>Creates a paragraph copying metadata from an existing one (filters).</summary>
    public TextParagraph(string text, IReadOnlyList<TextLine> lines, string originalText, bool wasShown)
    {
        Text = text;
        OriginalText = originalText;
        WasShown = wasShown;
        TextLines = lines;
        Lines = lines;
    }
}
