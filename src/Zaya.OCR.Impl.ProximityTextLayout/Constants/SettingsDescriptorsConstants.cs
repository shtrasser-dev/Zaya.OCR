using Zaya.Primitives;

namespace Zaya.OCR.Impl.ProximityTextLayout.Constants;

internal static class SettingsDescriptorsConstants
{
    // Compact glyphs (currentColor) for filter table headers — same idea as ScreenTranslator filters.
    private const string IconEnabled =
    """<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M18.36 6.64a9 9 0 1 1-12.73 0"/><line x1="12" y1="2" x2="12" y2="12"/></svg>""";

    private const string IconRegex =
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 508 368" fill="currentColor"><g transform="translate(0,368) scale(0.1,-0.1)"><path d="M2813 2763 c-62 -8 -63 -23 -16 -292 l6 -34 -114 92 c-63 50 -121 91 -131 91 -36 0 -119 -137 -105 -173 3 -8 67 -38 143 -68 105 -41 132 -55 118 -61 -11 -5 -73 -30 -139 -56 -80 -32 -121 -54 -123 -65 -8 -39 74 -167 106 -167 10 0 66 39 125 86 59 48 110 85 112 82 2 -2 -5 -58 -15 -123 -27 -162 -26 -173 8 -182 15 -5 58 -8 96 -7 96 1 97 4 75 151 -10 65 -21 130 -24 145 -4 25 7 19 110 -63 82 -65 121 -90 137 -87 37 5 109 136 95 172 -3 7 -67 37 -142 66 l-136 53 128 51 c150 60 153 61 153 87 0 45 -76 159 -107 159 -7 0 -64 -40 -125 -89 -62 -49 -113 -89 -114 -88 -1 1 9 65 21 142 12 77 20 145 17 152 -7 20 -95 35 -159 26z"/><path d="M1353 2752 c-32 -20 -145 -308 -189 -479 -47 -185 -58 -285 -58 -503 0 -231 14 -342 69 -544 52 -189 154 -426 190 -440 21 -8 178 -8 199 0 9 3 16 14 16 24 0 10 -27 99 -59 197 -91 273 -131 507 -131 768 0 283 52 570 150 825 45 120 48 135 21 150 -23 12 -190 13 -208 2z"/><path d="M3564 2746 c-19 -14 -19 -15 0 -63 129 -332 177 -581 177 -913 0 -325 -45 -556 -178 -913 -18 -47 -18 -49 1 -63 21 -15 191 -20 212 -6 17 11 87 156 127 263 142 380 166 821 67 1217 -46 183 -151 452 -187 480 -24 18 -193 16 -219 -2z"/><path d="M1903 1485 c-53 -23 -68 -57 -68 -155 0 -140 29 -170 160 -170 130 0 165 36 165 171 0 87 -19 131 -65 153 -41 20 -148 20 -192 1z"/></g></svg>""";

    public static readonly IReadOnlyList<SettingDescriptor> Settings =
    [
        new IntegerSettingDescriptor(SettingsConstants.WordGapThreshold, Loc(LocalizationConstants.Settings.WordGapThreshold))
        {
            Description = Loc(LocalizationConstants.Settings.WordGapThreshold_Desc),
            DefaultValue = 50,
            MinValue = 0,
        },
        new IntegerSettingDescriptor(SettingsConstants.BaselineDriftTolerance, Loc(LocalizationConstants.Settings.BaselineDriftTolerance))
        {
            Description = Loc(LocalizationConstants.Settings.BaselineDriftTolerance_Desc),
            DefaultValue = 50,
            MinValue = 0,
        },
        new IntegerSettingDescriptor(SettingsConstants.AngleToleranceDegrees, Loc(LocalizationConstants.Settings.AngleToleranceDegrees))
        {
            Description = Loc(LocalizationConstants.Settings.AngleToleranceDegrees_Desc),
            DefaultValue = 10,
            MinValue = 0,
            MaxValue = 90,
        },
        new IntegerSettingDescriptor(SettingsConstants.LineSpacingThreshold, Loc(LocalizationConstants.Settings.LineSpacingThreshold))
        {
            Description = Loc(LocalizationConstants.Settings.LineSpacingThreshold_Desc),
            DefaultValue = 150,
            MinValue = 0,
        },
        new IntegerSettingDescriptor(SettingsConstants.LineOverhangTolerancePercent, Loc(LocalizationConstants.Settings.LineOverhangTolerancePercent))
        {
            Description = Loc(LocalizationConstants.Settings.LineOverhangTolerancePercent_Desc),
            DefaultValue = 100,
            MinValue = 0,
        },
        new IntegerSettingDescriptor(SettingsConstants.FontSizeTolerance, Loc(LocalizationConstants.Settings.FontSizeTolerance))
        {
            Description = Loc(LocalizationConstants.Settings.FontSizeTolerance_Desc),
            DefaultValue = 50,
            MinValue = 0,
        },
        new BooleanSettingDescriptor(SettingsConstants.VerticalColumns, Loc(LocalizationConstants.Settings.VerticalColumns))
        {
            Description = Loc(LocalizationConstants.Settings.VerticalColumns_Desc),
            DefaultValue = false,
        },
        new TableSettingDescriptor(SettingsConstants.WordFilters, Loc(LocalizationConstants.Settings.WordFilters))
        {
            Description = Loc(LocalizationConstants.Settings.WordFilters_Desc),
            Columns = BuildFilterColumns(),
        },
        new TableSettingDescriptor(SettingsConstants.LineFilters, Loc(LocalizationConstants.Settings.LineFilters))
        {
            Description = Loc(LocalizationConstants.Settings.LineFilters_Desc),
            Columns = BuildFilterColumns(),
        },
        new TableSettingDescriptor(SettingsConstants.ParagraphFilters, Loc(LocalizationConstants.Settings.ParagraphFilters))
        {
            Description = Loc(LocalizationConstants.Settings.ParagraphFilters_Desc),
            Columns = BuildFilterColumns(),
        },
        new IntegerSettingDescriptor(SettingsConstants.CenterThresholdXPercent, Loc(LocalizationConstants.Settings.CenterThresholdXPercent))
        {
            Description = Loc(LocalizationConstants.Settings.CenterThresholdXPercent_Desc),
            DefaultValue = 300,
            MinValue = 0,
            MaxValue = 1000,
        },
        new IntegerSettingDescriptor(SettingsConstants.CenterThresholdYPercent, Loc(LocalizationConstants.Settings.CenterThresholdYPercent))
        {
            Description = Loc(LocalizationConstants.Settings.CenterThresholdYPercent_Desc),
            DefaultValue = 75,
            MinValue = 0,
            MaxValue = 500,
        },
        new IntegerSettingDescriptor(SettingsConstants.ParagraphMergeHysteresisPercent, Loc(LocalizationConstants.Settings.ParagraphMergeHysteresisPercent))
        {
            Description = Loc(LocalizationConstants.Settings.ParagraphMergeHysteresisPercent_Desc),
            DefaultValue = 120,
            MinValue = 100,
            MaxValue = 200,
        },
        new IntegerSettingDescriptor(SettingsConstants.SameLineWordGapHysteresisPercent, Loc(LocalizationConstants.Settings.SameLineWordGapHysteresisPercent))
        {
            Description = Loc(LocalizationConstants.Settings.SameLineWordGapHysteresisPercent_Desc),
            DefaultValue = 600,
            MinValue = 100,
            MaxValue = 2000,
        },
        new BooleanSettingDescriptor(SettingsConstants.EnableStabilization, Loc(LocalizationConstants.Settings.EnableStabilization))
        {
            Description = Loc(LocalizationConstants.Settings.EnableStabilization_Desc),
            DefaultValue = true,
        },
        new BooleanSettingDescriptor(SettingsConstants.HoldNewBlocks, Loc(LocalizationConstants.Settings.HoldNewBlocks))
        {
            Description = Loc(LocalizationConstants.Settings.HoldNewBlocks_Desc),
            DefaultValue = false,
            IsVisible = StabVisible,
        },
        new IntegerSettingDescriptor(SettingsConstants.LevenshteinThreshold, Loc(LocalizationConstants.Settings.LevenshteinThreshold))
        {
            Description = Loc(LocalizationConstants.Settings.LevenshteinThreshold_Desc),
            DefaultValue = 8,
            MinValue = 1,
            MaxValue = 50,
            IsVisible = StabVisible,
        },
        new IntegerSettingDescriptor(SettingsConstants.GhostMaxFrames, Loc(LocalizationConstants.Settings.GhostMaxFrames))
        {
            Description = Loc(LocalizationConstants.Settings.GhostMaxFrames_Desc),
            DefaultValue = 3,
            MinValue = 0,
            MaxValue = 30,
            IsVisible = StabVisible,
        },
    ];

    private static IReadOnlyList<SettingDescriptor> BuildFilterColumns() =>
    [
        new BooleanSettingDescriptor(SettingsConstants.FilterEnabled, Loc(LocalizationConstants.Settings.FilterEnabled))
        {
            DefaultValue = true,
            IconSvg = IconEnabled,
        },
        new StringSettingDescriptor(SettingsConstants.FilterPattern, Loc(LocalizationConstants.Settings.FilterPattern))
        {
            DefaultValue = string.Empty,
        },
        new BooleanSettingDescriptor(SettingsConstants.FilterIsRegex, Loc(LocalizationConstants.Settings.FilterIsRegex))
        {
            DefaultValue = false,
            IconSvg = IconRegex,
        },
        new EnumSettingDescriptor(SettingsConstants.FilterAction, Loc(LocalizationConstants.Settings.FilterAction))
        {
            DefaultValue = SettingsConstants.FilterActionSkip,
            Options =
            [
                new EnumOption(SettingsConstants.FilterActionSkip, Loc(LocalizationConstants.Settings.FilterActionSkip)),
                new EnumOption(SettingsConstants.FilterActionStrip, Loc(LocalizationConstants.Settings.FilterActionStrip)),
            ],
        },
        new StringSettingDescriptor(SettingsConstants.FilterDescription, Loc(LocalizationConstants.Settings.FilterDescription))
        {
            DefaultValue = string.Empty,
        },
    ];

    private static bool StabVisible(IReadOnlyDictionary<string, object?> s)
        => s.GetValueOrDefault(SettingsConstants.EnableStabilization) as bool? ?? true;

    private static LocalizedString Loc(string key)
        => new(key, culture => Properties.Resources.ResourceManager.GetString(key, culture)!);
}
