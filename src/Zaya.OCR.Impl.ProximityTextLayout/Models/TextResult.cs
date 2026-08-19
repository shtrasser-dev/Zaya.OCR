using Zaya.OCR.Models;
using Zaya.Primitives.OCR;

namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>
/// Working / frozen layout frame for ProximityTextLayout.
/// </summary>
public sealed class TextResult : ITextResult
{
    private List<TextWord> _words = [];
    private List<TextLine> _lines = [];
    private List<TextParagraph> _allParagraphs = [];
    private List<TextParagraph> _emittedParagraphs = [];
    private bool _frozen;

    /// <inheritdoc />
    public IReadOnlyList<ITextParagraph> Paragraphs => _emittedParagraphs;

    /// <summary>All paragraphs including held (non-emitted), for history.</summary>
    public IReadOnlyList<TextParagraph> AllParagraphs => _allParagraphs;

    /// <summary>
    /// Assembled layout lines for pipeline/history (not part of <see cref="ITextResult"/>).
    /// </summary>
    public IReadOnlyList<TextLine> AssembledLines => _lines;

    /// <summary>Mutable word list (before freeze).</summary>
    public List<TextWord> MutableWords
    {
        get
        {
            ThrowIfFrozen();
            return _words;
        }
    }

    /// <summary>Mutable line list (before freeze).</summary>
    public List<TextLine> MutableLines
    {
        get
        {
            ThrowIfFrozen();
            return _lines;
        }
        set
        {
            ThrowIfFrozen();
            _lines = value;
        }
    }

    /// <summary>Mutable paragraph list (before freeze) — includes held.</summary>
    public List<TextParagraph> MutableParagraphs
    {
        get
        {
            ThrowIfFrozen();
            return _allParagraphs;
        }
        set
        {
            ThrowIfFrozen();
            _allParagraphs = value;
        }
    }

    /// <inheritdoc />
    public string FullText => string.Join("\n\n", _emittedParagraphs.Select(p => p.Text));

    /// <summary>Creates an empty working frame.</summary>
    public TextResult()
    {
    }

    /// <summary>Creates a result from paragraphs only (compat / tests).</summary>
    public TextResult(IReadOnlyList<ITextParagraph> paragraphs)
    {
        _allParagraphs = paragraphs
            .Select(WrapParagraph)
            .ToList();
        foreach (var p in _allParagraphs)
        {
            p.IsEmitted = true;
            p.WasShown = true;
        }
        _emittedParagraphs = _allParagraphs.ToList();
        _lines = _allParagraphs.SelectMany(p => p.TextLines).ToList();
        _frozen = true;
    }

    /// <summary>Creates a working frame from words.</summary>
    public TextResult(IReadOnlyList<TextWord> words)
    {
        _words = words.ToList();
    }

    /// <summary>Locks lists; <see cref="Paragraphs"/> becomes emitted-only.</summary>
    public void Freeze()
    {
        if (_frozen)
            return;

        _words = _words.ToList();
        _lines = _lines.ToList();
        _allParagraphs = _allParagraphs.ToList();
        _emittedParagraphs = _allParagraphs.Where(p => p.IsEmitted).ToList();
        _frozen = true;
    }

    /// <summary>True after <see cref="Freeze"/>.</summary>
    public bool IsFrozen => _frozen;

    private static TextParagraph WrapParagraph(ITextParagraph p)
    {
        if (p is TextParagraph concrete)
            return concrete;

        var wrapped = new TextParagraph(p.Text, p.Lines) { Id = p.Id };
        if (p is ITextParagraphExt ext)
        {
            wrapped.HasPreviousFrameMatch = ext.HasPreviousFrameMatch;
            wrapped.PreviousFrameMatchAge = ext.PreviousFrameMatchAge;
            wrapped.PreviousFrameText = ext.PreviousFrameText;
            wrapped.IsGhost = ext.IsGhost;
            wrapped.GhostAge = ext.GhostAge;
        }

        return wrapped;
    }

    private void ThrowIfFrozen()
    {
        if (_frozen)
            throw new InvalidOperationException("TextResult is frozen.");
    }
}
