namespace Zaya.OCR.Models;

/// <summary>
/// Optional debug/tracking metadata for a layout line (e.g. overlay diagnostics).
/// Concrete layout engines may implement this alongside <see cref="Zaya.Primitives.OCR.ITextLine"/>.
/// </summary>
public interface ITextLineExt
{
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
    /// </summary>
    string PreviousFrameText { get; }
}
