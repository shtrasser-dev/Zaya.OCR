using System.Drawing;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Temporal paragraph stabilization across OCR frames.
/// </summary>
internal sealed class ParagraphStabilizer
{
    private readonly double _centerThresholdXFraction;
    private readonly double _centerThresholdYFraction;
    private readonly int _levenshteinThresholdPercent;
    private readonly int _minLength;
    private readonly double _lineSpacingThreshold;
    private readonly double _leftEdgeAlignmentTolerance;
    private readonly double _fontSizeTolerance;

    private IReadOnlyList<TrackedParagraph> _previous = [];

    public ParagraphStabilizer(
        double centerThresholdXFraction,
        double centerThresholdYFraction,
        int levenshteinThresholdPercent,
        int minLength,
        double lineSpacingThreshold = 1.5,
        double leftEdgeAlignmentTolerance = 1.0,
        double fontSizeTolerance = 0.5)
    {
        _centerThresholdXFraction = Math.Clamp(centerThresholdXFraction, 0, 10);
        _centerThresholdYFraction = Math.Clamp(centerThresholdYFraction, 0, 10);
        _levenshteinThresholdPercent = Math.Clamp(levenshteinThresholdPercent, 1, 50);
        _minLength = Math.Max(1, minLength);
        _lineSpacingThreshold = Math.Max(0, lineSpacingThreshold);
        _leftEdgeAlignmentTolerance = Math.Max(0, leftEdgeAlignmentTolerance);
        _fontSizeTolerance = Math.Max(0, fontSizeTolerance);
    }

    public IReadOnlyList<ITextParagraph> Stabilize(IReadOnlyList<ITextParagraph> incoming)
    {
        if (incoming.Count == 0)
            return StabilizeEmptyFrame();

        if (_previous.Count == 0)
        {
            _previous = [.. incoming.Select(p => TrackedParagraph.From(p, WasEmitted: true))];
            return incoming;
        }

        var used = new bool[_previous.Count];
        var result = new List<ITextParagraph>(incoming.Count);
        var remembered = new List<TrackedParagraph>(incoming.Count);
        var unmatchedIncoming = new List<TrackedParagraph>();

        foreach (var currentParagraph in incoming)
        {
            var current = TrackedParagraph.From(currentParagraph, WasEmitted: false);
            var bestIndex = -1;
            var bestCenterDist = double.MaxValue;
            var bestLev = int.MaxValue;

            var curHeight = AverageLineHeight(current.Paragraph);
            var maxDx = Math.Max(1.0, _centerThresholdXFraction * curHeight);
            var maxDy = Math.Max(1.0, _centerThresholdYFraction * curHeight);

            for (var i = 0; i < _previous.Count; i++)
            {
                if (used[i])
                    continue;

                var prev = _previous[i];
                var dx = Math.Abs(current.Center.X - prev.Center.X);
                var dy = Math.Abs(current.Center.Y - prev.Center.Y);
                if (dx > maxDx || dy > maxDy)
                    continue;

                if (!IsTextMatch(current.CompareKey, prev.CompareKey, out var lev))
                    continue;

                var centerDist = Math.Sqrt(dx * dx + dy * dy);
                if (centerDist < bestCenterDist
                    || (Math.Abs(centerDist - bestCenterDist) < 0.01 && lev < bestLev))
                {
                    bestCenterDist = centerDist;
                    bestLev = lev;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                unmatchedIncoming.Add(current);
                // Brand-new block: remember, do not emit until a later frame matches it.
                remembered.Add(current with { WasEmitted = false });
                continue;
            }

            used[bestIndex] = true;
            var tracked = _previous[bestIndex];

            // Prefer previous when lengths are equal — same-length OCR noise (enemjes/enemies)
            // must not rotate the remembered candidate and later "upgrade" to it.
            var longer = current.Normalized.Length > tracked.Normalized.Length ? current : tracked;

            if (string.Equals(current.CompareKey, tracked.CompareKey, StringComparison.Ordinal))
            {
                result.Add(tracked.Paragraph);
                remembered.Add(tracked with { WasEmitted = true, GhostMisses = 0 });
                continue;
            }

            // Text changed (grow / flicker). Keep showing if it was already emitted;
            // otherwise hold until content stabilizes across frames.
            if (tracked.WasEmitted)
            {
                result.Add(tracked.Paragraph);
                remembered.Add(longer with { WasEmitted = true, GhostMisses = 0 });
            }
            else
            {
                remembered.Add(longer with { WasEmitted = false, GhostMisses = 0 });
            }
        }

        AppendGhostsForUnused(used, unmatchedIncoming, result, remembered);
        SuppressEmitsWithPendingMergeCandidatesBelow(result, remembered);

        _previous = remembered;
        return result;
    }

    private IReadOnlyList<ITextParagraph> StabilizeEmptyFrame()
    {
        if (_previous.Count == 0)
        {
            _previous = [];
            return [];
        }

        var used = new bool[_previous.Count]; // all unused
        var result = new List<ITextParagraph>();
        var remembered = new List<TrackedParagraph>();
        AppendGhostsForUnused(used, unmatchedIncoming: [], result, remembered);
        _previous = remembered;
        return result;
    }

    /// <summary>
    /// Keep already-shown unmatched paragraphs for one more frame when appropriate:
    /// short texts always; long texts only when a spatial replacement candidate exists
    /// (strong text change in the same slot — avoids a blank frame while the new text waits).
    /// </summary>
    private void AppendGhostsForUnused(
        bool[] used,
        List<TrackedParagraph> unmatchedIncoming,
        List<ITextParagraph> result,
        List<TrackedParagraph> remembered)
    {
        for (var i = 0; i < _previous.Count; i++)
        {
            if (used[i])
                continue;

            var prev = _previous[i];
            if (!prev.WasEmitted || prev.GhostMisses > 0)
                continue;

            var isShort = prev.CompareKey.Length < _minLength;
            if (!isShort && !HasSpatialReplacement(prev, unmatchedIncoming))
                continue;

            result.Add(prev.Paragraph);
            remembered.Add(prev with { GhostMisses = 1 });
        }
    }

    private bool HasSpatialReplacement(TrackedParagraph prev, List<TrackedParagraph> unmatchedIncoming)
    {
        if (unmatchedIncoming.Count == 0)
            return false;

        var prevHeight = AverageLineHeight(prev.Paragraph);
        var maxDxPrev = Math.Max(1.0, _centerThresholdXFraction * prevHeight);
        var maxDyPrev = Math.Max(1.0, _centerThresholdYFraction * prevHeight);

        foreach (var candidate in unmatchedIncoming)
        {
            var candHeight = AverageLineHeight(candidate.Paragraph);
            var maxDx = Math.Max(maxDxPrev, Math.Max(1.0, _centerThresholdXFraction * candHeight));
            var maxDy = Math.Max(maxDyPrev, Math.Max(1.0, _centerThresholdYFraction * candHeight));
            var dx = Math.Abs(prev.Center.X - candidate.Center.X);
            var dy = Math.Abs(prev.Center.Y - candidate.Center.Y);
            if (dx <= maxDx && dy <= maxDy)
                return true;
        }

        return false;
    }

    /// <summary>
    /// If a ready paragraph has a not-yet-emitted paragraph below that could still merge
    /// (same vertical band + same horizontal region), hold the upper paragraph this frame.
    /// Does not clear <see cref="TrackedParagraph.WasEmitted"/> — unrelated flicker must not
    /// force the upper block through re-confirmation.
    /// </summary>
    private void SuppressEmitsWithPendingMergeCandidatesBelow(
        List<ITextParagraph> result,
        List<TrackedParagraph> remembered)
    {
        var pending = remembered
            .Where(t => !t.WasEmitted)
            .Select(t => t.Paragraph)
            .ToList();
        if (pending.Count == 0 || result.Count == 0)
            return;

        for (var i = result.Count - 1; i >= 0; i--)
        {
            var emitted = result[i];
            if (pending.Any(p => IsLikelyFutureMerge(emitted, p)))
                result.RemoveAt(i);
        }
    }

    /// <summary>
    /// <paramref name="lower"/> sits below <paramref name="upper"/> within line-spacing and
    /// font-size tolerances, and shares the same horizontal region (overlap or under the upper box).
    /// Vertical-only checks falsely couple unrelated columns on the same screen.
    /// When <paramref name="lower"/> is an expanded multi-line pending paragraph, its first line
    /// may overlap the upper slot — then a later line is treated as the merge-below candidate.
    /// </summary>
    private bool IsLikelyFutureMerge(ITextParagraph upper, ITextParagraph lower)
    {
        if (upper.Lines.Count == 0 || lower.Lines.Count == 0)
            return false;

        var upperLast = upper.Lines[^1];
        var upperCenterY = upperLast.Bounds.Y + upperLast.Bounds.Height / 2.0;
        var upperBounds = UnionBounds(upper);
        var lowerBounds = UnionBounds(lower);

        foreach (var lowerLine in lower.Lines)
        {
            if (IsLikelyFutureMergeLine(upperLast, upperCenterY, upperBounds, lowerLine, lowerBounds))
                return true;
        }

        return false;
    }

    private bool IsLikelyFutureMergeLine(
        ITextLine upperLast,
        double upperCenterY,
        Rectangle upperBounds,
        ITextLine lowerLine,
        Rectangle lowerBounds)
    {
        var lowerCenterY = lowerLine.Bounds.Y + lowerLine.Bounds.Height / 2.0;
        if (lowerCenterY <= upperCenterY)
            return false;

        var avgHeight = (upperLast.Bounds.Height + lowerLine.Bounds.Height) / 2.0;

        // Same-slot OCR replacement sits within a fraction of a line — not a merge-below candidate.
        if (lowerCenterY - upperCenterY < avgHeight * 0.5)
            return false;

        var maxGap = Math.Max(1.0, _lineSpacingThreshold * avgHeight);
        if (lowerCenterY - upperCenterY > maxGap)
            return false;

        var heightDiff = Math.Abs(upperLast.Bounds.Height - lowerLine.Bounds.Height);
        if (heightDiff > _fontSizeTolerance * avgHeight)
            return false;

        return SharesHorizontalRegion(upperBounds, lowerBounds, avgHeight);
    }

    private bool SharesHorizontalRegion(Rectangle upper, Rectangle lower, double avgHeight)
    {
        // Direct overlap — typical continuation of the same block.
        if (upper.Left < lower.Right && lower.Left < upper.Right)
            return true;

        // Lower line centered / growing under the upper box (centered UI text).
        var margin = Math.Max(_leftEdgeAlignmentTolerance * avgHeight, upper.Width * 0.5);
        var lowerCenterX = lower.Left + lower.Width / 2.0;
        return lowerCenterX >= upper.Left - margin
               && lowerCenterX <= upper.Right + margin;
    }

    public void Reset() => _previous = [];

    private readonly record struct TrackedParagraph(
        ITextParagraph Paragraph,
        bool WasEmitted,
        string Normalized,
        string CompareKey,
        (double X, double Y) Center,
        int GhostMisses = 0)
    {
        public static TrackedParagraph From(ITextParagraph paragraph, bool WasEmitted)
        {
            var normalized = Normalize(JoinText(paragraph));
            return new TrackedParagraph(
                paragraph,
                WasEmitted,
                normalized,
                NormalizeForCompare(normalized),
                ComputeCenter(paragraph));
        }
    }

    private bool IsTextMatch(string aCompareKey, string bCompareKey, out int distance)
    {
        if (string.Equals(aCompareKey, bCompareKey, StringComparison.Ordinal))
        {
            distance = 0;
            return true;
        }

        if (aCompareKey.Length < _minLength || bCompareKey.Length < _minLength)
        {
            distance = int.MaxValue;
            return false;
        }

        var longerLen = Math.Max(aCompareKey.Length, bCompareKey.Length);
        var allowed = Math.Max(1, (int)Math.Floor(longerLen * (_levenshteinThresholdPercent / 100.0)));
        distance = Levenshtein(aCompareKey, bCompareKey);
        return distance <= allowed;
    }

    internal static string JoinText(ITextParagraph paragraph)
    {
        if (paragraph.Lines.Count > 0)
            return string.Join(" ", paragraph.Lines.Select(l => l.Text));
        return paragraph.Text.Replace('\r', ' ').Replace('\n', ' ');
    }

    internal static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new System.Text.StringBuilder(text.Length);
        var prevSpace = false;
        foreach (var ch in text.Trim())
        {
            var c = ch switch
            {
                '\u2018' or '\u2019' or '\u2032' => '\'',
                '\u201C' or '\u201D' or '\u2033' => '"',
                '\u00A0' or '\u2007' or '\u202F' => ' ',
                _ => ch,
            };

            if (c is '\u200B' or '\u200C' or '\u200D' or '\uFEFF')
                continue;

            if (char.IsWhiteSpace(c))
            {
                if (prevSpace)
                    continue;
                sb.Append(' ');
                prevSpace = true;
            }
            else
            {
                sb.Append(c);
                prevSpace = false;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Aggressive skeleton for similarity / equality only (not for display or length upgrades):
    /// lowercase, strip whitespace and <c>.</c> <c>,</c> quotes / apostrophes.
    /// </summary>
    internal static string NormalizeForCompare(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sb = new System.Text.StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
                continue;

            var c = ch switch
            {
                '\u2018' or '\u2019' or '\u2032' => '\'',
                '\u201C' or '\u201D' or '\u2033' => '"',
                _ => ch,
            };

            if (c is '.' or ',' or '"' or '\'')
                continue;

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    private static (double X, double Y) ComputeCenter(ITextParagraph paragraph)
    {
        var bounds = UnionBounds(paragraph);
        return (bounds.X + bounds.Width / 2.0, bounds.Y + bounds.Height / 2.0);
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

    private static double AverageLineHeight(ITextParagraph paragraph)
    {
        if (paragraph.Lines.Count == 0)
            return 1;

        return Math.Max(1.0, paragraph.Lines.Average(l => (double)l.Bounds.Height));
    }

    internal static int Levenshtein(string a, string b)
    {
        if (a.Length == 0)
            return b.Length;
        if (b.Length == 0)
            return a.Length;

        var prev = new int[b.Length + 1];
        var curr = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                curr[j] = Math.Min(
                    Math.Min(curr[j - 1] + 1, prev[j] + 1),
                    prev[j - 1] + cost);
            }

            (prev, curr) = (curr, prev);
        }

        return prev[b.Length];
    }
}
