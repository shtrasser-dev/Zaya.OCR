using System.Drawing;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Models;
using Zaya.OCR.Services;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// ProximityTextLayout session that merges OCR words into paragraphs.
/// </summary>
public sealed class ProximityTextLayoutSession : ITextLayoutSession
{
    private readonly ProximityTextLayoutOptions _options;
    private readonly ParagraphStabilizer? _stabilizer;
    private readonly double _paragraphMergeHysteresis;
    private readonly LayoutTextFilter _wordFilter;
    private readonly LayoutTextFilter _lineFilter;
    private readonly LayoutTextFilter _paragraphFilter;
    private IReadOnlyList<ITextParagraph> _lastEmitted = [];
    private bool _disposed;

    internal ProximityTextLayoutSession(
        ProximityTextLayoutOptions options,
        LayoutTextFilter? wordFilter = null,
        LayoutTextFilter? lineFilter = null,
        LayoutTextFilter? paragraphFilter = null)
    {
        _options = options;
        _paragraphMergeHysteresis = Math.Clamp(options.ParagraphMergeHysteresis, 1.0, 3.0);
        _wordFilter = wordFilter ?? LayoutTextFilter.Empty;
        _lineFilter = lineFilter ?? LayoutTextFilter.Empty;
        _paragraphFilter = paragraphFilter ?? LayoutTextFilter.Empty;
        if (options.EnableStabilization)
        {
            _stabilizer = new ParagraphStabilizer(
                options.CenterThresholdXFraction,
                options.CenterThresholdYFraction,
                options.LevenshteinThresholdPercent,
                options.MinStabilizationLength,
                options.LineSpacingThreshold,
                options.LeftEdgeAlignmentTolerance,
                options.FontSizeTolerance);
        }
    }

    /// <inheritdoc />
    public Task<ITextResult> ProcessAsync(IOCRResult result, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var words = _wordFilter.FilterWords(result.Words);

        if (words.Count == 0)
        {
            // Let the stabilizer emit short-paragraph ghosts for one empty frame when enabled.
            if (_stabilizer is not null)
            {
                var ghosts = _stabilizer.Stabilize([]).ToList();
                _lastEmitted = ghosts;
                return Task.FromResult<ITextResult>(new TextResult(ghosts));
            }

            _lastEmitted = [];
            return Task.FromResult<ITextResult>(new TextResult([]));
        }

        var clusters = ClusterWords(words);

        cancellationToken.ThrowIfCancellationRequested();
        var lines = new List<ITextLine>();
        foreach (var cluster in clusters)
        {
            var clusterLines = GroupWordsIntoLines(cluster);
            lines.AddRange(clusterLines);
        }

        lines = _lineFilter.FilterLines(lines).ToList();

        cancellationToken.ThrowIfCancellationRequested();
        var paragraphs = GroupLinesIntoParagraphs(lines);
        paragraphs = _paragraphFilter.FilterParagraphs(paragraphs).ToList();
        if (_stabilizer is not null)
            paragraphs = _stabilizer.Stabilize(paragraphs).ToList();

        _lastEmitted = paragraphs;
        return Task.FromResult<ITextResult>(new TextResult(paragraphs));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _stabilizer?.Reset();
        _lastEmitted = [];
    }

    private List<List<IOCRWord>> ClusterWords(IReadOnlyList<IOCRWord> words)
    {
        var sorted = words
            .OrderBy(w => w.Bounds.Y + w.Bounds.Height / 2.0)
            .ToList();

        var clusters = new List<List<IOCRWord>>();
        if (sorted.Count == 0)
            return clusters;

        var currentCluster = new List<IOCRWord> { sorted[0] };
        var lastWord = sorted[0];

        for (int i = 1; i < sorted.Count; i++)
        {
            var word = sorted[i];
            var lastCenterY = lastWord.Bounds.Y + lastWord.Bounds.Height / 2.0;
            var wordCenterY = word.Bounds.Y + word.Bounds.Height / 2.0;
            var avgHeight = (lastWord.Bounds.Height + word.Bounds.Height) / 2.0;
            var maxDrift = _options.BaselineDriftTolerance * avgHeight;

            if (Math.Abs(wordCenterY - lastCenterY) <= maxDrift)
            {
                currentCluster.Add(word);
            }
            else
            {
                currentCluster.Sort((a, b) => a.Bounds.Left.CompareTo(b.Bounds.Left));
                clusters.Add(currentCluster);
                currentCluster = new List<IOCRWord> { word };
            }

            lastWord = word;
        }

        currentCluster.Sort((a, b) => a.Bounds.Left.CompareTo(b.Bounds.Left));
        clusters.Add(currentCluster);

        clusters.Sort((a, b) =>
        {
            var aCenterY = a[0].Bounds.Y + a[0].Bounds.Height / 2.0;
            var bCenterY = b[0].Bounds.Y + b[0].Bounds.Height / 2.0;
            return aCenterY.CompareTo(bCenterY);
        });

        return clusters;
    }

    private List<ITextLine> GroupWordsIntoLines(List<IOCRWord> clusterWords)
    {
        var lines = new List<ITextLine>();
        var currentLineWords = new List<IOCRWord>();

        foreach (var word in clusterWords)
        {
            if (currentLineWords.Count == 0)
            {
                currentLineWords.Add(word);
                continue;
            }

            var lastWord = currentLineWords[^1];
            var gap = word.Bounds.Left - lastWord.Bounds.Right;
            var avgHeight = (word.Bounds.Height + lastWord.Bounds.Height) / 2.0;
            var maxGap = _options.WordGapThreshold * avgHeight;

            var wordCenterY = word.Bounds.Y + word.Bounds.Height / 2.0;
            var lastCenterY = lastWord.Bounds.Y + lastWord.Bounds.Height / 2.0;
            var centerDiff = Math.Abs(wordCenterY - lastCenterY);
            var maxDrift = _options.BaselineDriftTolerance * avgHeight;

            if (gap <= maxGap && centerDiff <= maxDrift)
            {
                currentLineWords.Add(word);
            }
            else
            {
                lines.Add(CreateTextLine(currentLineWords));
                currentLineWords.Clear();
                currentLineWords.Add(word);
            }
        }

        if (currentLineWords.Count > 0)
            lines.Add(CreateTextLine(currentLineWords));

        return lines;
    }

    private static TextLine CreateTextLine(List<IOCRWord> lineWords)
    {
        var text = string.Join(" ", lineWords.Select(w => w.Text));

        var minX = lineWords.Min(w => w.Bounds.Left);
        var minY = lineWords.Min(w => w.Bounds.Top);
        var maxX = lineWords.Max(w => w.Bounds.Right);
        var maxY = lineWords.Max(w => w.Bounds.Bottom);

        var bounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);
        return new TextLine(text, lineWords.ToList(), bounds);
    }

    private List<ITextParagraph> GroupLinesIntoParagraphs(List<ITextLine> lines)
    {
        var paragraphBuckets = new List<List<ITextLine>>();

        foreach (var line in lines)
        {
            bool matched = false;

            for (int i = paragraphBuckets.Count - 1; i >= 0; i--)
            {
                var bucket = paragraphBuckets[i];
                var lastLine = bucket[^1];
                var scale = GetMergeToleranceScale(lastLine, line);

                if (!CanMergeLines(bucket, line, scale))
                    continue;

                bucket.Add(line);
                matched = true;
                break;
            }

            if (!matched)
                paragraphBuckets.Add(new List<ITextLine> { line });
        }

        return paragraphBuckets.Select(CreateTextParagraph).Cast<ITextParagraph>().ToList();
    }

    private bool CanMergeLines(List<ITextLine> bucket, ITextLine line, double scale)
    {
        var lastLine = bucket[^1];

        var lineCenterY = line.Bounds.Y + line.Bounds.Height / 2.0;
        var lastCenterY = lastLine.Bounds.Y + lastLine.Bounds.Height / 2.0;
        var avgHeight = (line.Bounds.Height + lastLine.Bounds.Height) / 2.0;

        var verticalGap = Math.Abs(lineCenterY - lastCenterY);
        var maxVerticalGap = _options.LineSpacingThreshold * avgHeight * scale;
        if (verticalGap > maxVerticalGap)
            return false;

        var heightDiff = Math.Abs(line.Bounds.Height - lastLine.Bounds.Height);
        var maxHeightDiff = _options.FontSizeTolerance * avgHeight * scale;
        if (heightDiff > maxHeightDiff)
            return false;

        return IsHorizontallyAligned(bucket, line, avgHeight, scale);
    }

    /// <summary>
    /// Bias line→paragraph merge toward the previous emitted structure:
    /// looser when both lines sat in one paragraph, tighter when they were separate.
    /// </summary>
    private double GetMergeToleranceScale(ITextLine upper, ITextLine lower)
    {
        if (_paragraphMergeHysteresis <= 1.0001 || _lastEmitted.Count == 0)
            return 1.0;

        var bias = GetMergeBias(upper, lower);
        return bias switch
        {
            MergeBias.PreferMerge => _paragraphMergeHysteresis,
            MergeBias.PreferSplit => 1.0 / _paragraphMergeHysteresis,
            _ => 1.0,
        };
    }

    private MergeBias GetMergeBias(ITextLine upper, ITextLine lower)
    {
        // Same previous multi-line paragraph covered both → keep them together.
        foreach (var prev in _lastEmitted)
        {
            if (prev.Lines.Count < 2)
                continue;
            if (ParagraphCoversLine(prev, upper) && ParagraphCoversLine(prev, lower))
                return MergeBias.PreferMerge;
        }

        // Distinct previous paragraphs covered each line → keep them apart.
        var upperHit = FindCoveringParagraph(upper);
        var lowerHit = FindCoveringParagraph(lower);
        if (upperHit is not null
            && lowerHit is not null
            && !ReferenceEquals(upperHit, lowerHit))
            return MergeBias.PreferSplit;

        return MergeBias.Neutral;
    }

    private ITextParagraph? FindCoveringParagraph(ITextLine line)
    {
        ITextParagraph? best = null;
        var bestArea = double.MaxValue;

        foreach (var prev in _lastEmitted)
        {
            if (!ParagraphCoversLine(prev, line))
                continue;

            var bounds = UnionBounds(prev);
            var area = (double)Math.Max(1, bounds.Width) * Math.Max(1, bounds.Height);
            if (area < bestArea)
            {
                bestArea = area;
                best = prev;
            }
        }

        return best;
    }

    private bool ParagraphCoversLine(ITextParagraph paragraph, ITextLine line)
    {
        var bounds = UnionBounds(paragraph);
        var h = Math.Max(1.0, line.Bounds.Height);
        var padX = Math.Max(1.0, _options.CenterThresholdXFraction * h);
        var padY = Math.Max(1.0, _options.CenterThresholdYFraction * h);
        var cx = line.Bounds.X + line.Bounds.Width / 2.0;
        var cy = line.Bounds.Y + line.Bounds.Height / 2.0;

        return cx >= bounds.Left - padX
               && cx <= bounds.Right + padX
               && cy >= bounds.Top - padY
               && cy <= bounds.Bottom + padY;
    }

    private bool IsHorizontallyAligned(List<ITextLine> paragraphLines, ITextLine newLine, double avgHeight, double scale)
    {
        var maxLeftDiff = _options.LeftEdgeAlignmentTolerance * avgHeight * scale;

        var referenceLeft = GetReferenceLeft(paragraphLines);
        var leftDiff = Math.Abs(newLine.Bounds.Left - referenceLeft);

        if (leftDiff <= maxLeftDiff)
            return true;

        if (paragraphLines.Count == 1)
        {
            var firstLine = paragraphLines[0];
            var indent = firstLine.Bounds.Left - newLine.Bounds.Left;

            if (indent > 0 && indent <= _options.FirstLineIndentTolerance * avgHeight * scale)
                return true;
        }

        if (_options.EnableCenterAlignment)
        {
            var refCenterX = referenceLeft + GetReferenceWidth(paragraphLines) / 2.0;
            var newCenterX = newLine.Bounds.Left + newLine.Bounds.Width / 2.0;
            var centerDiff = Math.Abs(newCenterX - refCenterX);

            if (centerDiff <= maxLeftDiff)
                return true;
        }

        return false;
    }

    private static double GetReferenceLeft(List<ITextLine> paragraphLines)
    {
        if (paragraphLines.Count == 1)
            return paragraphLines[0].Bounds.Left;

        return paragraphLines[1].Bounds.Left;
    }

    private static double GetReferenceWidth(List<ITextLine> paragraphLines)
    {
        if (paragraphLines.Count == 1)
            return paragraphLines[0].Bounds.Width;

        return paragraphLines[1].Bounds.Width;
    }

    private static Rectangle UnionBounds(ITextParagraph paragraph)
    {
        if (paragraph.Lines.Count == 0)
            return Rectangle.Empty;

        var minX = paragraph.Lines.Min(l => l.Bounds.Left);
        var minY = paragraph.Lines.Min(l => l.Bounds.Top);
        var maxX = paragraph.Lines.Max(l => l.Bounds.Right);
        var maxY = paragraph.Lines.Max(l => l.Bounds.Bottom);
        return Rectangle.FromLTRB(minX, minY, maxX, maxY);
    }

    private static TextParagraph CreateTextParagraph(List<ITextLine> paragraphLines)
    {
        var text = string.Join("\n", paragraphLines.Select(l => l.Text));
        return new TextParagraph(text, paragraphLines.ToList());
    }

    private enum MergeBias
    {
        Neutral,
        PreferMerge,
        PreferSplit,
    }
}
