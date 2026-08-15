using System.Text;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services.Impl;

/// <summary>Text normalization helpers for compare keys (not display).</summary>
internal static class TextNormalize
{
    /// <summary>Collapse whitespace and unify quotes; keep casing for length/display comparisons.</summary>
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
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

    /// <summary>Aggressive compare key: lower-case, strip spaces and light punctuation.</summary>
    public static string ForCompare(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
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

    /// <summary>Compare-key from display text.</summary>
    public static string CompareKey(string displayText) => ForCompare(Normalize(displayText));

    /// <summary>Levenshtein distance.</summary>
    public static int Levenshtein(string a, string b)
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

    /// <summary>Fuzzy match on compare keys (no short-text special case).</summary>
    public static bool FuzzyMatch(
        string aCompareKey,
        string bCompareKey,
        int levenshteinThresholdPercent)
    {
        if (string.Equals(aCompareKey, bCompareKey, StringComparison.Ordinal))
            return true;

        var longerLen = Math.Max(aCompareKey.Length, bCompareKey.Length);
        if (longerLen == 0)
            return true;

        var allowed = Math.Max(1, (int)Math.Floor(longerLen * (levenshteinThresholdPercent / 100.0)));
        return Levenshtein(aCompareKey, bCompareKey) <= allowed;
    }
}
