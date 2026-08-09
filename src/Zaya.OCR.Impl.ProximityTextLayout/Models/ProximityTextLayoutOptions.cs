namespace Zaya.OCR.Impl.ProximityTextLayout.Models;

/// <summary>
/// Options for configuring <see cref="Services.ProximityTextLayoutSession"/>.
/// </summary>
public sealed record ProximityTextLayoutOptions(
    double WordGapThreshold,
    double BaselineDriftTolerance,
    double LineSpacingThreshold,
    double LeftEdgeAlignmentTolerance,
    double FirstLineIndentTolerance,
    bool EnableCenterAlignment,
    double FontSizeTolerance,
    bool EnableStabilization = true,
    double CenterThresholdXFraction = 3.0,
    double CenterThresholdYFraction = 0.75,
    int LevenshteinThresholdPercent = 8,
    double ParagraphMergeHysteresis = 1.2,
    double AngleToleranceDegrees = 10,
    bool HoldNewBlocks = false,
    double MaxLineProtrusionFraction = 0.10,
    int GhostMaxFrames = 3,
    double SameLineWordGapHysteresis = 6.0,
    bool VerticalColumns = false);
