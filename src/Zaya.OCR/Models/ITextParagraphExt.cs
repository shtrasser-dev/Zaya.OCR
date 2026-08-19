namespace Zaya.OCR.Models;

/// <summary>
/// Optional debug/tracking and ghost metadata for a layout paragraph (e.g. overlay diagnostics).
/// Concrete layout engines may implement this alongside <see cref="Zaya.Primitives.OCR.ITextParagraph"/>.
/// </summary>
public interface ITextParagraphExt
{
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
