using System.Drawing;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Temporal paragraph stabilization across OCR frames.
/// </summary>
internal sealed class ParagraphStabilizer
{
    private readonly double _centerThresholdFraction;
    private readonly int _levenshteinThresholdPercent;
    private readonly int _minLength;

    private IReadOnlyList<ITextParagraph> _previous = [];

    public ParagraphStabilizer(
        double centerThresholdFraction,
        int levenshteinThresholdPercent,
        int minLength)
    {
        _centerThresholdFraction = Math.Clamp(centerThresholdFraction, 0, 10);
        _levenshteinThresholdPercent = Math.Clamp(levenshteinThresholdPercent, 1, 50);
        _minLength = Math.Max(1, minLength);
    }

    public IReadOnlyList<ITextParagraph> Stabilize(IReadOnlyList<ITextParagraph> incoming)
    {
        if (incoming.Count == 0)
        {
            _previous = [];
            return incoming;
        }

        if (_previous.Count == 0)
        {
            _previous = incoming;
            return incoming;
        }

        var used = new bool[_previous.Count];
        var result = new List<ITextParagraph>(incoming.Count);

        foreach (var current in incoming)
        {
            var bestIndex = -1;
            var bestCenterDist = double.MaxValue;
            var bestLev = int.MaxValue;

            var curNorm = Normalize(JoinText(current));
            var curCenter = Center(current);
            var curHeight = AverageLineHeight(current);
            var maxCenterDist = Math.Max(1.0, _centerThresholdFraction * curHeight);

            for (var i = 0; i < _previous.Count; i++)
            {
                if (used[i])
                    continue;

                var prev = _previous[i];
                var prevNorm = Normalize(JoinText(prev));
                var centerDist = Distance(curCenter, Center(prev));
                if (centerDist > maxCenterDist)
                    continue;

                if (!IsTextMatch(curNorm, prevNorm, out var lev))
                    continue;

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
                result.Add(current);
                continue;
            }

            used[bestIndex] = true;
            var matched = _previous[bestIndex];
            var matchedNorm = Normalize(JoinText(matched));

            // Equal or shorter new text → keep previous paragraph entirely.
            // Longer new text → take the new paragraph with new coordinates.
            result.Add(curNorm.Length <= matchedNorm.Length ? matched : current);
        }

        _previous = result;
        return result;
    }

    public void Reset() => _previous = [];

    private bool IsTextMatch(string a, string b, out int distance)
    {
        if (string.Equals(a, b, StringComparison.Ordinal))
        {
            distance = 0;
            return true;
        }

        if (a.Length < _minLength || b.Length < _minLength)
        {
            distance = int.MaxValue;
            return false;
        }

        var longerLen = Math.Max(a.Length, b.Length);
        var allowed = Math.Max(1, (int)Math.Floor(longerLen * (_levenshteinThresholdPercent / 100.0)));
        distance = Levenshtein(a, b);
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

    private static (double X, double Y) Center(ITextParagraph paragraph)
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

    private static double Distance((double X, double Y) a, (double X, double Y) b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
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
