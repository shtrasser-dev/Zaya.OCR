namespace Zaya.OCR.Models;

/// <summary>
/// Represents a paragraph of text composed of one or more lines.
/// </summary>
public interface ITextParagraph
{
    /// <summary>
    /// Stable identity for this paragraph across frames.
    /// When the paragraph matches a previous-frame paragraph, equals that paragraph's <see cref="Id"/>;
    /// otherwise a newly generated unique value.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the full text of this paragraph, with lines separated by newlines.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets the lines that make up this paragraph.
    /// </summary>
    IReadOnlyList<ITextLine> Lines { get; }

    /// <summary>
    /// True when this paragraph was matched to a previous-frame paragraph (geometry / tracking).
    /// </summary>
    bool HasPreviousFrameMatch { get; }

    /// <summary>
    /// How many frames this paragraph identity has been alive (1 on first appearance;
    /// increments while <see cref="HasPreviousFrameMatch"/> stays true across frames).
    /// </summary>
    int PreviousFrameMatchAge { get; }

    /// <summary>
    /// Display text of the matched previous-frame paragraph, or empty when there is no match.
    /// Hosts can restore the old text-equality signal with
    /// <c>HasPreviousFrameMatch &amp;&amp; string.Equals(Text, PreviousFrameText, StringComparison.OrdinalIgnoreCase)</c>.
    /// </summary>
    string PreviousFrameText { get; }

    /// <summary>
    /// True when this paragraph is carried forward as a ghost from a previous frame
    /// (no live match this frame).
    /// </summary>
    bool IsGhost { get; }

    /// <summary>
    /// Consecutive ghost frames for this identity (0 = live emit; increments while <see cref="IsGhost"/>).
    /// </summary>
    int GhostAge { get; }
}
