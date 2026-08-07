using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Models;
using Zaya.OCR.Services;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// ProximityTextLayout session: filters → lines → paragraphs → emit → ghost.
/// </summary>
public sealed class ProximityTextLayoutSession : ITextLayoutSession
{
    private readonly ProximityTextLayoutOptions _options;
    private readonly LayoutTextFilter _wordFilter;
    private readonly LayoutTextFilter _lineFilter;
    private readonly LayoutTextFilter _paragraphFilter;
    private readonly TextLayoutHistoryService _history;
    private readonly LineAssembler _lineAssembler;
    private readonly ParagraphAssembler _paragraphAssembler;
    private readonly ParagraphTextEmitter _textEmitter;
    private readonly ParagraphGhostService _ghostService;
    private bool _disposed;

    internal ProximityTextLayoutSession(
        ProximityTextLayoutOptions options,
        LayoutTextFilter? wordFilter = null,
        LayoutTextFilter? lineFilter = null,
        LayoutTextFilter? paragraphFilter = null)
    {
        _options = options;
        _wordFilter = wordFilter ?? LayoutTextFilter.Empty;
        _lineFilter = lineFilter ?? LayoutTextFilter.Empty;
        _paragraphFilter = paragraphFilter ?? LayoutTextFilter.Empty;
        _history = new TextLayoutHistoryService(
            options.AngleToleranceDegrees,
            alongTolFraction: options.CenterThresholdXFraction,
            acrossTolFraction: options.CenterThresholdYFraction);
        _lineAssembler = new LineAssembler(options);
        _paragraphAssembler = new ParagraphAssembler(options);
        _textEmitter = new ParagraphTextEmitter(options);
        _ghostService = new ParagraphGhostService(options);
    }

    /// <inheritdoc />
    public Task<ITextResult> ProcessAsync(IOCRResult result, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var filteredWords = _wordFilter.FilterWords(result.Words);
        var words = filteredWords.Select(w => w as TextWord ?? new TextWord(w)).ToList();
        var frame = new TextResult(words);

        if (words.Count == 0)
        {
            _ghostService.AppendGhosts(frame, _history);
            frame.Freeze();
            _history.Push(frame);
            return Task.FromResult<ITextResult>(frame);
        }

        _lineAssembler.Assemble(frame, _history);
        ApplyLineFilter(frame);
        cancellationToken.ThrowIfCancellationRequested();

        _paragraphAssembler.Assemble(frame, _history);
        _textEmitter.Emit(frame, _history);
        ApplyParagraphFilter(frame);
        _ghostService.AppendGhosts(frame, _history);

        frame.Freeze();
        _history.Push(frame);
        return Task.FromResult<ITextResult>(frame);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _history.Clear();
    }

    private void ApplyLineFilter(TextResult frame)
    {
        if (_lineFilter.IsEmpty)
            return;

        var filtered = _lineFilter.FilterLines(frame.MutableLines).ToList();
        var concrete = new List<TextLine>(filtered.Count);
        foreach (var line in filtered)
        {
            if (line is TextLine tl)
            {
                concrete.Add(tl);
                continue;
            }

            concrete.Add(new TextLine(line.Text, line.Words, line.Bounds));
        }

        frame.MutableLines = concrete;
    }

    private void ApplyParagraphFilter(TextResult frame)
    {
        if (_paragraphFilter.IsEmpty)
            return;

        var filtered = _paragraphFilter.FilterParagraphs(frame.MutableParagraphs).ToList();
        var concrete = new List<TextParagraph>(filtered.Count);
        foreach (var p in filtered)
        {
            if (p is TextParagraph tp)
            {
                concrete.Add(tp);
                continue;
            }

            concrete.Add(new TextParagraph(p.Text, p.Lines.OfType<TextLine>().ToList()));
        }

        frame.MutableParagraphs = concrete;
    }
}
