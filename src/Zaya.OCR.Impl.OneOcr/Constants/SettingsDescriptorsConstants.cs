using Zaya.Primitives;
using Zaya.Primitives.Settings;

namespace Zaya.OCR.Impl.OneOcr.Constants;

internal static class SettingsDescriptorsConstants
{
    private const string DefaultCacheCompany = "Zaya";
    private const string DefaultCacheProduct = "OneOcr";

    private const string DefaultDownloadUrl = @"https://github.com/shtrasser-dev/Zaya.External/releases/latest/download/OneOCR.zip";

    public static readonly IReadOnlyList<SettingDescriptor> Settings =
    [
        new EnumSettingDescriptor(SettingsConstants.Source, Loc(LocalizationConstants.Settings.Source))
        {
            Description = Loc(LocalizationConstants.Settings.Source_Desc),
            IsRequired = static _ => true,
            DefaultValue = SettingsConstants.Auto,
            Options =
            [
                new(SettingsConstants.Auto,         Loc(LocalizationConstants.Settings.Source_Auto)),
                new(SettingsConstants.SnippingTool, Loc(LocalizationConstants.Settings.Source_SnippingTool)),
                new(SettingsConstants.Directory,    Loc(LocalizationConstants.Settings.Source_Directory)),
                new(SettingsConstants.Url,          Loc(LocalizationConstants.Settings.Source_Url)),
            ]
        },
        new DirectoryPathSettingDescriptor(SettingsConstants.DirectoryPath, Loc(LocalizationConstants.Settings.EngineDir))
        {
            Description = Loc(LocalizationConstants.Settings.EngineDir_Desc),
            IsVisible  = s => s.GetValueOrDefault(SettingsConstants.Source) as string == SettingsConstants.Directory,
            IsRequired = s => s.GetValueOrDefault(SettingsConstants.Source) as string == SettingsConstants.Directory,
        },
        new UrlSettingDescriptor(SettingsConstants.DownloadUrl, Loc(LocalizationConstants.Settings.DownloadUrl))
        {
            Description = Loc(LocalizationConstants.Settings.DownloadUrl_Desc),
            DefaultValue = DefaultDownloadUrl,
            IsVisible  = s => (s.GetValueOrDefault(SettingsConstants.Source) as string ?? SettingsConstants.Auto) is SettingsConstants.Url,
            IsRequired = s => s.GetValueOrDefault(SettingsConstants.Source) as string == SettingsConstants.Url,
        },
        new DirectoryPathSettingDescriptor(SettingsConstants.CacheDirectory, Loc(LocalizationConstants.Settings.CacheDir))
        {
            Description = Loc(LocalizationConstants.Settings.CacheDir_Desc),
            DefaultValue = Path.Combine(Path.GetTempPath(), DefaultCacheCompany, DefaultCacheProduct),
            IsVisible  = s => (s.GetValueOrDefault(SettingsConstants.Source) as string ?? SettingsConstants.Auto) is SettingsConstants.Auto or SettingsConstants.SnippingTool or SettingsConstants.Url,
        },
        new IntegerSettingDescriptor(SettingsConstants.MinConfidence, Loc(LocalizationConstants.Settings.MinConfidence))
        {
            Description = Loc(LocalizationConstants.Settings.MinConfidence_Desc),
            DefaultValue = 70,
            MinValue = 0,
            MaxValue = 100,
        }
    ];

    private static LocalizedString Loc(string key)
        => new(key, culture => Properties.Resources.ResourceManager.GetString(key, culture)!);
}
