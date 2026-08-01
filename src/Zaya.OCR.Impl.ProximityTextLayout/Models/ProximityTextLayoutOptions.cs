namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>
/// Options for configuring <see cref="Services.ProximityTextLayoutSession"/>.
/// </summary>
/// <param name="WordGapThreshold">
/// Multiplier applied to the average word height to determine the maximum horizontal gap
/// allowed between two words on the same line. Higher values merge words that are further apart.
/// </param>
/// <param name="BaselineDriftTolerance">
/// Multiplier applied to the average word height to determine the maximum vertical drift
/// allowed between words that are considered to be on the same baseline.
/// Higher values tolerate greater vertical misalignment.
/// </param>
/// <param name="LineSpacingThreshold">
/// Multiplier applied to the average line height to determine the maximum vertical gap
/// allowed between consecutive lines within the same paragraph.
/// Higher values merge lines that are further apart vertically.
/// </param>
/// <param name="LeftEdgeAlignmentTolerance">
/// Multiplier applied to the average line height to determine how closely the left edges
/// of lines must align to be considered part of the same paragraph.
/// Higher values allow greater horizontal misalignment.
/// </param>
/// <param name="FirstLineIndentTolerance">
/// Multiplier applied to the average line height to determine the maximum allowed indent
/// of the first line of a paragraph relative to subsequent lines.
/// Higher values tolerate deeper first-line indentation.
/// </param>
/// <param name="EnableCenterAlignment">
/// When <c>true</c>, lines whose centers are horizontally aligned are also considered
/// part of the same paragraph. Useful for centered or heading text.
/// </param>
/// <param name="FontSizeTolerance">
/// Multiplier applied to the average line height to determine the maximum allowed font size
/// difference between lines within the same paragraph.
/// Higher values tolerate greater size variation.
/// </param>
/// <param name="EnableStabilization">
/// When <c>true</c>, paragraphs are temporally matched to the previous frame to reduce OCR flicker.
/// </param>
/// <param name="CenterThresholdFraction">
/// Max center-point drift as a fraction of average paragraph line height (e.g. 0.5 = 50%).
/// </param>
/// <param name="LevenshteinThresholdPercent">
/// Max Levenshtein distance as a percent of the longer normalized text length.
/// </param>
/// <param name="MinStabilizationLength">
/// Below this normalized length, only exact text matches are accepted for temporal pairing.
/// </param>
public sealed record ProximityTextLayoutOptions(
    double WordGapThreshold,
    double BaselineDriftTolerance,
    double LineSpacingThreshold,
    double LeftEdgeAlignmentTolerance,
    double FirstLineIndentTolerance,
    bool EnableCenterAlignment,
    double FontSizeTolerance,
    bool EnableStabilization = true,
    double CenterThresholdFraction = 0.5,
    int LevenshteinThresholdPercent = 8,
    int MinStabilizationLength = 16);
