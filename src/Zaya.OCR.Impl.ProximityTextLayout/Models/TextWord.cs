using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>Internal word wrapper used by the layout pipeline.</summary>
public sealed class TextWord : ITextWord, IOCRWord
{
    /// <inheritdoc />
    public string Text { get; }

    /// <inheritdoc />
    public BoundingBox Bounds { get; }

    /// <inheritdoc />
    public double Confidence { get; }

    /// <summary>Previous word on the same line (along reading), if any.</summary>
    public TextWord? PrevWord { get; set; }

    /// <summary>Next word on the same line (along reading), if any.</summary>
    public TextWord? NextWord { get; set; }

    /// <summary>Creates a word from an OCR word.</summary>
    public TextWord(IOCRWord source)
        : this(source.Text, source.Bounds, source.Confidence)
    {
    }

    /// <summary>Creates a word with explicit fields.</summary>
    public TextWord(string text, BoundingBox bounds, double confidence)
    {
        Text = text;
        Bounds = bounds;
        Confidence = confidence;
    }
}
