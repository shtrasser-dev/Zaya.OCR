using System.Text;
using Zaya.OCR.Models;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services.Impl;

/// <summary>Helpers for manga vertical-column mode (CJK + upright punctuation/digits).</summary>
internal static class CjkText
{
    private const double NearSquareMaxRelativeDiff = 0.15;

    /// <summary>
    /// True when this OCR word should use vertical reading direction under <c>verticalColumns</c>:
    /// CJK text; a single punctuation mark or digit; or an all-digit number with a nearly square box.
    /// </summary>
    public static bool ShouldRelabelForVerticalColumns(string text, BoundingBox bounds)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        if (ContainsCjk(text))
            return true;

        var trimmed = text.Trim();
        if (trimmed.Length == 0)
            return false;

        var runes = trimmed.EnumerateRunes().ToList();
        if (runes.Count == 1)
        {
            var r = runes[0];
            if (IsVerticalColumnPunctuation(r) || Rune.IsDigit(r))
                return true;
        }

        if (runes.TrueForAll(Rune.IsDigit) && IsNearlySquare(bounds))
            return true;

        return false;
    }

    /// <summary>
    /// True when <paramref name="text"/> contains hiragana, katakana, or CJK ideographs/punctuation.
    /// </summary>
    public static bool ContainsCjk(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var rune in text.EnumerateRunes())
        {
            if (IsCjk(rune))
                return true;
        }

        return false;
    }

    public static bool IsCjk(Rune rune)
    {
        var value = rune.Value;
        return value is (>= 0x3040 and <= 0x30FF)   // Hiragana + Katakana
            or (>= 0x3400 and <= 0x4DBF)           // CJK Ext A
            or (>= 0x4E00 and <= 0x9FFF)           // CJK Unified
            or (>= 0xF900 and <= 0xFAFF)           // CJK Compatibility Ideographs
            or (>= 0x3000 and <= 0x303F)           // CJK Symbols and Punctuation
            or (>= 0xFF65 and <= 0xFF9F);          // Halfwidth katakana
    }

    /// <summary>
    /// Question / exclamation / period-like marks (ASCII and common fullwidth/CJK forms).
    /// </summary>
    public static bool IsVerticalColumnPunctuation(Rune rune)
    {
        var v = rune.Value;
        return v is '.' or '!' or '?'
            or '。' or '、' or '．' or '！' or '？'
            or '･' or '・' or '…' or '⋯';
    }

    /// <summary>
    /// True when AABB width and height differ by at most 15% of the smaller side.
    /// </summary>
    public static bool IsNearlySquare(BoundingBox bounds)
    {
        var w = Math.Max(1e-3, bounds.MaxX - bounds.MinX);
        var h = Math.Max(1e-3, bounds.MaxY - bounds.MinY);
        var min = Math.Min(w, h);
        var max = Math.Max(w, h);
        return (max - min) / min <= NearSquareMaxRelativeDiff;
    }

    /// <summary>
    /// Relabels corners so reading <see cref="BoundingBox.Direction"/> is top→bottom,
    /// without moving the glyph box in image space (OCR boxes stay aligned with upright glyphs).
    /// Axis-aligned OneOCR order P1=TL,P2=TR,P3=BR,P4=BL becomes P1=TL,P2=BL,P3=BR,P4=TR.
    /// </summary>
    public static BoundingBox RelabelForVerticalReading(BoundingBox box)
        => new(box.P1, box.P4, box.P3, box.P2);
}
