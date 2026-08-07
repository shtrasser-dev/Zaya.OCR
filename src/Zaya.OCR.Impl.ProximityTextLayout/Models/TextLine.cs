using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>Internal line with temporal links and display rails.</summary>
public sealed class TextLine : ITextLine
{
    private readonly List<TextLine> _previousFrameLineList = [];

    /// <inheritdoc />
    public string Text { get; set; }

    /// <inheritdoc />
    public IReadOnlyList<IOCRWord> Words { get; }

    /// <inheritdoc />
    public BoundingBox Bounds { get; set; }

    /// <inheritdoc />
    public bool HasPreviousFrameMatch
    {
        get
        {
            if (_previousFrameLineList.Count == 0)
                return false;

            var previousText = string.Join(" ", _previousFrameLineList.Select(p => p.Text));
            return string.Equals(Text, previousText, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Normalized compare key (optional cache).</summary>
    public string? CompareKey { get; set; }

    /// <summary>Previous line in the current-frame paragraph (down the normal).</summary>
    public TextLine? PrevLine { get; set; }

    /// <summary>Next line in the current-frame paragraph.</summary>
    public TextLine? NextLine { get; set; }

    /// <summary>
    /// Matched previous-frame lines, sorted left→right along reading direction.
    /// Empty = new line.
    /// </summary>
    public IList<TextLine> PreviousFrameLineList => _previousFrameLineList;

    /// <summary>Creates a line.</summary>
    public TextLine(string text, IReadOnlyList<IOCRWord> words, BoundingBox bounds)
    {
        Text = text;
        Words = words;
        Bounds = bounds;
    }

    /// <summary>Replaces the previous-frame list and sorts left→right along <paramref name="direction"/>.</summary>
    public void SetPreviousFrameLines(IEnumerable<TextLine> previous, System.Numerics.Vector2 direction)
    {
        _previousFrameLineList.Clear();
        _previousFrameLineList.AddRange(previous);
        SortPreviousFrameLineList(direction);
    }

    /// <summary>Sorts <see cref="PreviousFrameLineList"/> left→right along <paramref name="direction"/>.</summary>
    public void SortPreviousFrameLineList(System.Numerics.Vector2 direction)
    {
        if (_previousFrameLineList.Count <= 1)
            return;

        _previousFrameLineList.Sort((a, b) =>
        {
            var ca = (a.Bounds.P5 + a.Bounds.P6) * 0.5f;
            var cb = (b.Bounds.P5 + b.Bounds.P6) * 0.5f;
            return System.Numerics.Vector2.Dot(ca, direction)
                .CompareTo(System.Numerics.Vector2.Dot(cb, direction));
        });
    }
}
