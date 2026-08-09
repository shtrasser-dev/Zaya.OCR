using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Appends previous-frame paragraphs that have no match and no overlap (ghosts).
/// </summary>
internal sealed class ParagraphGhostService
{
    private readonly ProximityTextLayoutOptions _options;

    public ParagraphGhostService(ProximityTextLayoutOptions options)
    {
        _options = options;
    }

    public void AppendGhosts(TextResult frame, TextLayoutHistoryService history)
    {
        if (!_options.EnableStabilization || history.Previous is null)
            return;

        if (_options.GhostMaxFrames <= 0)
            return;

        var maxAge = _options.GhostMaxFrames;
        var claimedByEmitted = new HashSet<TextLine>();
        foreach (var paragraph in frame.MutableParagraphs.Where(p => p.IsEmitted))
        {
            foreach (var line in paragraph.TextLines)
            {
                foreach (var prev in line.PreviousFrameLineList)
                    claimedByEmitted.Add(prev);
            }
        }

        var currentEmitted = frame.MutableParagraphs.Where(p => p.IsEmitted).ToList();

        foreach (var prevParagraph in history.Previous.AllParagraphs)
        {
            if (!prevParagraph.IsEmitted && !prevParagraph.IsGhost)
                continue;

            if (prevParagraph.TextLines.Any(l => claimedByEmitted.Contains(l)))
                continue;

            if (OverlapsAny(prevParagraph, currentEmitted))
                continue;

            var age = prevParagraph.IsGhost ? prevParagraph.GhostAge + 1 : 1;
            if (age > maxAge)
                continue;

            // Clone ghost into current frame Stable set; keep stable ids and advance ages.
            var ghostLines = prevParagraph.TextLines
                .Select(l => new TextLine(
                    l.Text,
                    l.Words,
                    l.Bounds,
                    l.Id,
                    hasPreviousFrameMatch: true,
                    previousFrameMatchAge: l.PreviousFrameMatchAge + 1,
                    previousFrameText: l.Text))
                .ToList();
            var ghost = new TextParagraph(
                prevParagraph.Text,
                ghostLines,
                prevParagraph.OriginalText,
                wasShown: true,
                id: prevParagraph.Id,
                hasPreviousFrameMatch: true,
                previousFrameMatchAge: prevParagraph.PreviousFrameMatchAge + 1,
                previousFrameText: prevParagraph.Text,
                isGhost: true,
                ghostAge: age)
            {
                IsEmitted = true,
            };
            frame.MutableParagraphs.Add(ghost);
        }
    }

    private static bool OverlapsAny(TextParagraph candidate, List<TextParagraph> others)
    {
        var cb = UnionBounds(candidate);
        if (cb.IsEmpty)
            return false;

        foreach (var other in others)
        {
            var ob = UnionBounds(other);
            if (ob.IsEmpty)
                continue;
            if (Intersects(cb, ob))
                return true;
        }

        return false;
    }

    private static BoundingBox UnionBounds(TextParagraph paragraph)
    {
        if (paragraph.TextLines.Count == 0)
            return BoundingBox.Empty;

        var minX = paragraph.TextLines.Min(l => l.Bounds.MinX);
        var minY = paragraph.TextLines.Min(l => l.Bounds.MinY);
        var maxX = paragraph.TextLines.Max(l => l.Bounds.MaxX);
        var maxY = paragraph.TextLines.Max(l => l.Bounds.MaxY);
        return BoundingBox.FromAxisAligned(minX, minY, maxX - minX, maxY - minY);
    }

    private static bool Intersects(BoundingBox a, BoundingBox b)
        => a.MinX <= b.MaxX && a.MaxX >= b.MinX && a.MinY <= b.MaxY && a.MaxY >= b.MinY;
}
