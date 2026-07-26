using Zaya.OCR.Impl.ProximityTextLayout.Constants;
using Zaya.OCR.Impl.ProximityTextLayout.Models;
using Zaya.OCR.Services;
using Zaya.Primitives;

namespace Zaya.OCR.Impl.ProximityTextLayout.Services;

/// <summary>
/// Proximity-based implementation of <see cref="ITextLayoutService"/>.
/// Merges individual OCR words into structured text blocks using configurable distance heuristics.
/// </summary>
public sealed class ProximityTextLayoutService : ITextLayoutService
{
    private const string EngineIdValue = "proximity-text-layout";

    private bool _disposed;

    private static IReadOnlyList<SettingDescriptor> _settings = [
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
        new IntegerSettingDescriptor(SettingsConstants.LineSpacingThreshold, Loc(LocalizationConstants.Settings.LineSpacingThreshold))
    {
        Description = Loc(LocalizationConstants.Settings.LineSpacingThreshold_Desc),
            DefaultValue = 150,
            MinValue = 0,
        },
        new IntegerSettingDescriptor(SettingsConstants.LeftEdgeAlignmentTolerance, Loc(LocalizationConstants.Settings.LeftEdgeAlignmentTolerance))
    {
        Description = Loc(LocalizationConstants.Settings.LeftEdgeAlignmentTolerance_Desc),
            DefaultValue = 100,
            MinValue = 0,
        },
        new IntegerSettingDescriptor(SettingsConstants.FirstLineIndentTolerance, Loc(LocalizationConstants.Settings.FirstLineIndentTolerance))
    {
        Description = Loc(LocalizationConstants.Settings.FirstLineIndentTolerance_Desc),
            DefaultValue = 300,
            MinValue = 0,
        },
        new IntegerSettingDescriptor(SettingsConstants.FontSizeTolerance, Loc(LocalizationConstants.Settings.FontSizeTolerance))
    {
        Description = Loc(LocalizationConstants.Settings.FontSizeTolerance_Desc),
            DefaultValue = 50,
            MinValue = 0,
        },
        new BooleanSettingDescriptor(SettingsConstants.EnableCenterAlignment, Loc(LocalizationConstants.Settings.CenterAlignment))
    {
        Description = Loc(LocalizationConstants.Settings.CenterAlignment_Desc),
            DefaultValue = false,
        },
    ];

    private static LocalizedString Loc(string key)
        => new(key, culture => Properties.Resources.ResourceManager.GetString(key, culture)!);

    /// <inheritdoc />
    public string EngineId => EngineIdValue;

    /// <inheritdoc />
    public LocalizedString DisplayName => Loc(LocalizationConstants.Settings.EngineName);

    /// <inheritdoc />
    public LocalizedString Description => Loc(LocalizationConstants.Settings.EngineDesc);

    /// <inheritdoc />
    public IReadOnlyList<SettingDescriptor> Settings { get; } = _settings;

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public async Task<ITextLayoutSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var settingDescriptorList = new SettingDescriptorList(_settings);
        return await CreateSessionAsync(settingDescriptorList, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ITextLayoutSession> CreateSessionAsync(IReadOnlyDictionary<string, object> engineSettings, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var settingDescriptorList = new SettingDescriptorList(_settings);
        settingDescriptorList.Bind(engineSettings);

        return await CreateSessionAsync(settingDescriptorList, cancellationToken);
    }

    private Task<ITextLayoutSession> CreateSessionAsync(SettingDescriptorList settingDescriptorList, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var options = new ProximityTextLayoutOptions(
            settingDescriptorList.GetValueAsInt(SettingsConstants.WordGapThreshold) / 100.0,
            settingDescriptorList.GetValueAsInt(SettingsConstants.BaselineDriftTolerance) / 100.0,
            settingDescriptorList.GetValueAsInt(SettingsConstants.LineSpacingThreshold) / 100.0,
            settingDescriptorList.GetValueAsInt(SettingsConstants.LeftEdgeAlignmentTolerance) / 100.0,
            settingDescriptorList.GetValueAsInt(SettingsConstants.FirstLineIndentTolerance) / 100.0,
            settingDescriptorList.GetValueAsBool(SettingsConstants.EnableCenterAlignment),
            settingDescriptorList.GetValueAsInt(SettingsConstants.FontSizeTolerance) / 100.0);

        return Task.FromResult<ITextLayoutSession>(new ProximityTextLayoutSession(options));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
