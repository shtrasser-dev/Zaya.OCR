using System.Diagnostics;
using System.Text.RegularExpressions;
using Zaya.OCR.Impl.ProximityTextLayout.Constants;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Models;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Pattern filters applied at word / line / paragraph stages of layout.
/// Non-regex patterns use case-insensitive full-string equality; regex uses invariant ignore-case match/replace.
/// </summary>
internal sealed class LayoutTextFilter
{
    public static LayoutTextFilter Empty { get; } = new([], []);

    private readonly IReadOnlyList<CompiledRule> _stripRules;
    private readonly IReadOnlyList<CompiledRule> _skipRules;

    private LayoutTextFilter(IReadOnlyList<CompiledRule> stripRules, IReadOnlyList<CompiledRule> skipRules)
    {
        _stripRules = stripRules;
        _skipRules = skipRules;
    }

    public bool IsEmpty => _stripRules.Count == 0 && _skipRules.Count == 0;

    public static LayoutTextFilter FromTable(IReadOnlyList<SettingDescriptorList> rows)
    {
        if (rows.Count == 0)
            return Empty;

        var strip = new List<CompiledRule>();
        var skip = new List<CompiledRule>();

        foreach (var row in rows)
        {
            if (!row.GetValueAsBool(SettingsConstants.FilterEnabled))
                continue;

            var pattern = row.GetValueAsString(SettingsConstants.FilterPattern);
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            var isRegex = row.GetValueAsBool(SettingsConstants.FilterIsRegex);
            var action = row.GetValueAsString(SettingsConstants.FilterAction);

            Regex? regex = null;
            if (isRegex)
            {
                try
                {
                    regex = new Regex(
                        pattern,
                        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
                }
                catch (ArgumentException ex)
                {
                    Debug.WriteLine($"[LayoutTextFilter] Invalid regex '{pattern}': {ex.Message}");
                    continue;
                }
            }

            var rule = new CompiledRule(pattern, regex);
            if (string.Equals(action, SettingsConstants.FilterActionStrip, StringComparison.OrdinalIgnoreCase))
                strip.Add(rule);
            else
                skip.Add(rule);
        }

        return strip.Count == 0 && skip.Count == 0
            ? Empty
            : new LayoutTextFilter(strip, skip);
    }

    /// <summary>Test helper: build a filter from explicit rules.</summary>
    internal static LayoutTextFilter FromRules(
        IEnumerable<(string Pattern, bool IsRegex, bool Strip)> rules)
    {
        var strip = new List<CompiledRule>();
        var skip = new List<CompiledRule>();
        foreach (var (pattern, isRegex, isStrip) in rules)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            Regex? regex = null;
            if (isRegex)
            {
                regex = new Regex(
                    pattern,
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            var rule = new CompiledRule(pattern, regex);
            if (isStrip)
                strip.Add(rule);
            else
                skip.Add(rule);
        }

        return strip.Count == 0 && skip.Count == 0
            ? Empty
            : new LayoutTextFilter(strip, skip);
    }

    public IReadOnlyList<IOCRWord> FilterWords(IReadOnlyList<IOCRWord> words)
    {
        if (IsEmpty || words.Count == 0)
            return words;

        var result = new List<IOCRWord>(words.Count);
        foreach (var word in words)
        {
            var text = Apply(word.Text);
            if (text is null)
                continue;
            if (string.Equals(text, word.Text, StringComparison.Ordinal))
                result.Add(word);
            else
                result.Add(new FilteredOcrWord(text, word.Bounds, word.Confidence));
        }

        return result;
    }

    public IReadOnlyList<ITextLine> FilterLines(IReadOnlyList<ITextLine> lines)
    {
        if (IsEmpty || lines.Count == 0)
            return lines;

        var result = new List<ITextLine>(lines.Count);
        foreach (var line in lines)
        {
            var text = Apply(line.Text);
            if (text is null)
                continue;
            if (string.Equals(text, line.Text, StringComparison.Ordinal))
            {
                result.Add(line);
            }
            else if (line is Models.TextLine concrete)
            {
                concrete.Text = text;
                result.Add(concrete);
            }
            else
            {
                result.Add(new Models.TextLine(text, line.Words, line.Bounds));
            }
        }

        return result;
    }

    public IReadOnlyList<ITextParagraph> FilterParagraphs(IReadOnlyList<ITextParagraph> paragraphs)
    {
        if (IsEmpty || paragraphs.Count == 0)
            return paragraphs;

        var result = new List<ITextParagraph>(paragraphs.Count);
        foreach (var paragraph in paragraphs)
        {
            var text = Apply(paragraph.Text);
            if (text is null)
                continue;
            if (string.Equals(text, paragraph.Text, StringComparison.Ordinal))
            {
                result.Add(paragraph);
            }
            else if (paragraph is Models.TextParagraph concrete)
            {
                concrete.Text = text;
                result.Add(concrete);
            }
            else
            {
                result.Add(new Models.TextParagraph(text, paragraph.Lines));
            }
        }

        return result;
    }

    /// <summary>
    /// Returns <c>null</c> when the text should be dropped; otherwise the (possibly stripped) text.
    /// </summary>
    private string? Apply(string original)
    {
        if (string.IsNullOrWhiteSpace(original))
            return null;

        var text = original;
        foreach (var rule in _stripRules)
            text = Strip(text, rule);

        text = text.Trim();
        if (text.Length == 0)
            return null;

        if (_skipRules.Any(r => Matches(text, r)))
            return null;

        return text;
    }

    private static string Strip(string text, CompiledRule rule)
    {
        if (rule.Regex is not null)
            return rule.Regex.Replace(text, string.Empty);

        // Non-regex: full-string equality only.
        return string.Equals(text, rule.Pattern, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : text;
    }

    private static bool Matches(string text, CompiledRule rule)
    {
        if (rule.Regex is not null)
            return rule.Regex.IsMatch(text);

        return string.Equals(text, rule.Pattern, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CompiledRule(string Pattern, Regex? Regex);

    private sealed class FilteredOcrWord(string text, BoundingBox bounds, double confidence) : IOCRWord
    {
        public string Text { get; } = text;
        public BoundingBox Bounds { get; } = bounds;
        public double Confidence { get; } = confidence;
    }
}
