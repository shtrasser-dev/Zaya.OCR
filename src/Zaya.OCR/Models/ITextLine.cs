namespace Zaya.OCR.Models;

/// <summary>
/// Represents a single line of text composed of one or more recognized words.
/// </summary>
public interface ITextLine
{
    /// <summary>
    /// Stable identity for this line across frames.
    /// When the line matches a previous-frame line, equals that line's <see cref="Id"/>;
    /// otherwise a newly generated unique value.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the concatenated text of all words in this line.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets the original recognized words that belong to this line.
    /// </summary>
    IReadOnlyList<IOCRWord> Words { get; }

    /// <summary>
    /// Gets the oriented bounding box that encompasses all words in this line.
    /// </summary>
    BoundingBox Bounds { get; }

    /// <summary>
    /// True when this line was matched to a previous-frame line (geometry / tracking).
    /// </summary>
    bool HasPreviousFrameMatch { get; }

    /// <summary>
    /// How many frames this line identity has been alive (1 on first appearance;
    /// increments while <see cref="HasPreviousFrameMatch"/> stays true across frames).
    /// </summary>
    int PreviousFrameMatchAge { get; }

    /// <summary>
    /// Display text of the matched previous-frame line(s), or empty when there is no match.
    /// Hosts can restore the old text-equality signal with
    /// <c>HasPreviousFrameMatch &amp;&amp; string.Equals(Text, PreviousFrameText, StringComparison.OrdinalIgnoreCase)</c>.
    /// </summary>
    string PreviousFrameText { get; }
}
