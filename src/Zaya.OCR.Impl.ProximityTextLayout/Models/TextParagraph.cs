using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>Internal paragraph with emit/ghost metadata.</summary>
public sealed class TextParagraph : ITextParagraph
{
    /// <inheritdoc />
    public Guid Id { get; set; } = Guid.NewGuid();

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

    /// <inheritdoc />
    public bool HasPreviousFrameMatch { get; set; }

    /// <inheritdoc />
    public int PreviousFrameMatchAge { get; set; } = 1;

    /// <inheritdoc />
    public string PreviousFrameText { get; set; } = string.Empty;

    /// <summary>True when included in the Stable emit list this frame.</summary>
    public bool IsEmitted { get; set; }

    /// <summary>
    /// True when this paragraph was shown to the user (emitted or ghosted as visible).
    /// Persists in history for <c>HoldNewBlocks</c> decisions on the next frame.
    /// </summary>
    public bool WasShown { get; set; }

    /// <inheritdoc />
    public bool IsGhost { get; set; }

    /// <inheritdoc />
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
        {
            concrete = lines.Select(l => l as TextLine
                ?? new TextLine(
                    l.Text,
                    l.Words,
                    l.Bounds,
                    l.Id,
                    l.HasPreviousFrameMatch,
                    l.PreviousFrameMatchAge,
                    l.PreviousFrameText)).ToList();
        }

        TextLines = concrete;
        Lines = concrete;
    }

    /// <summary>Creates a paragraph copying metadata from an existing one (filters / ghosts).</summary>
    public TextParagraph(
        string text,
        IReadOnlyList<TextLine> lines,
        string originalText,
        bool wasShown,
        Guid? id = null,
        bool hasPreviousFrameMatch = false,
        int previousFrameMatchAge = 1,
        string previousFrameText = "",
        bool isGhost = false,
        int ghostAge = 0)
    {
        Text = text;
        OriginalText = originalText;
        WasShown = wasShown;
        if (id is { } existing)
            Id = existing;
        HasPreviousFrameMatch = hasPreviousFrameMatch;
        PreviousFrameMatchAge = previousFrameMatchAge;
        PreviousFrameText = previousFrameText;
        IsGhost = isGhost;
        GhostAge = ghostAge;
        TextLines = lines;
        Lines = lines;
    }
}
