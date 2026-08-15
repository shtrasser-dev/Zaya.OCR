using Zaya.Logging.Services;
using Zaya.OCR.Impl.ProximityTextLayout.Constants;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Impl.ProximityTextLayout.Services;
using Zaya.OCR.Impl.ProximityTextLayout.Services.Impl;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.ProximityTextLayout;

/// <summary>
/// Proximity-based implementation of <see cref="ITextLayoutService"/>.
/// Merges individual OCR words into structured text blocks using configurable distance heuristics.
/// </summary>
public sealed class ProximityTextLayoutService : ITextLayoutService
{
    private const string EngineIdValue = "proximity-text-layout";

    private readonly ILoggingWrapper _loggingWrapper;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance using <see cref="EmptyLoggingWrapper.Instance"/>.
    /// </summary>
    public ProximityTextLayoutService() : this(EmptyLoggingWrapper.Instance)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified logging wrapper.
    /// </summary>
    /// <param name="loggingWrapper">Logging wrapper used when creating sessions.</param>
    public ProximityTextLayoutService(ILoggingWrapper loggingWrapper)
    {
        _loggingWrapper = loggingWrapper;
    }

    private static LocalizedString Loc(string key)
        => new(key, culture => Properties.Resources.ResourceManager.GetString(key, culture)!);

    /// <inheritdoc />
    public string EngineId => EngineIdValue;

    /// <inheritdoc />
    public LocalizedString DisplayName => Loc(LocalizationConstants.Settings.EngineName);

    /// <inheritdoc />
    public LocalizedString Description => Loc(LocalizationConstants.Settings.EngineDesc);

    /// <inheritdoc />
    public IReadOnlyList<SettingDescriptor> Settings { get; } = SettingsDescriptorsConstants.Settings;

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public async Task<ITextLayoutSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var settingDescriptorList = new SettingDescriptorList(SettingsDescriptorsConstants.Settings);
        return await CreateSessionAsync(settingDescriptorList, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ITextLayoutSession> CreateSessionAsync(
        IReadOnlyDictionary<string, object> engineSettings,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var settingDescriptorList = new SettingDescriptorList(SettingsDescriptorsConstants.Settings);
        settingDescriptorList.Bind(engineSettings);

        return await CreateSessionAsync(settingDescriptorList, cancellationToken);
    }

    private Task<ITextLayoutSession> CreateSessionAsync(
        SettingDescriptorList settingDescriptorList,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var options = new ProximityTextLayoutOptions(
            WordGapThreshold: settingDescriptorList.GetValueAsInt(SettingsConstants.WordGapThreshold) / 100.0,
            BaselineDriftTolerance: settingDescriptorList.GetValueAsInt(SettingsConstants.BaselineDriftTolerance) / 100.0,
            LineSpacingThreshold: settingDescriptorList.GetValueAsInt(SettingsConstants.LineSpacingThreshold) / 100.0,
            LineOverhangTolerance: settingDescriptorList.GetValueAsInt(SettingsConstants.LineOverhangTolerancePercent) / 100.0,
            FontSizeTolerance: settingDescriptorList.GetValueAsInt(SettingsConstants.FontSizeTolerance) / 100.0,
            EnableStabilization: settingDescriptorList.GetValueAsBool(SettingsConstants.EnableStabilization),
            CenterThresholdXFraction: settingDescriptorList.GetValueAsInt(SettingsConstants.CenterThresholdXPercent) / 100.0,
            CenterThresholdYFraction: settingDescriptorList.GetValueAsInt(SettingsConstants.CenterThresholdYPercent) / 100.0,
            LevenshteinThresholdPercent: settingDescriptorList.GetValueAsInt(SettingsConstants.LevenshteinThreshold),
            ParagraphMergeHysteresis: settingDescriptorList.GetValueAsInt(SettingsConstants.ParagraphMergeHysteresisPercent) / 100.0,
            AngleToleranceDegrees: settingDescriptorList.GetValueAsInt(SettingsConstants.AngleToleranceDegrees),
            HoldNewBlocks: settingDescriptorList.GetValueAsBool(SettingsConstants.HoldNewBlocks),
            GhostMaxFrames: settingDescriptorList.GetValueAsInt(SettingsConstants.GhostMaxFrames),
            SameLineWordGapHysteresis: settingDescriptorList.GetValueAsInt(SettingsConstants.SameLineWordGapHysteresisPercent) / 100.0,
            VerticalColumns: settingDescriptorList.GetValueAsBool(SettingsConstants.VerticalColumns));

        var wordFilter = _loggingWrapper.Wrap<ILayoutTextFilter>(LayoutTextFilter.FromTable(settingDescriptorList.GetValueAsTable(SettingsConstants.WordFilters)));
        var lineFilter = _loggingWrapper.Wrap<ILayoutTextFilter>(LayoutTextFilter.FromTable(settingDescriptorList.GetValueAsTable(SettingsConstants.LineFilters)));
        var paragraphFilter = _loggingWrapper.Wrap<ILayoutTextFilter>(LayoutTextFilter.FromTable(settingDescriptorList.GetValueAsTable(SettingsConstants.ParagraphFilters)));

        var session = new ProximityTextLayoutSession(options, _loggingWrapper, wordFilter, lineFilter, paragraphFilter);
        return Task.FromResult(_loggingWrapper.Wrap<ITextLayoutSession>(session));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
