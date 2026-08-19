using Windows.Media.Ocr;
using Zaya.Primitives;
using Zaya.Primitives.Settings;

namespace Zaya.OCR.Impl.WindowsMediaOcr.Constants;

internal static class SettingsDescriptorsConstants
{
    /// <summary>
    /// Builds engine settings. Language options are resolved at call time from the OS.
    /// </summary>
    public static IReadOnlyList<SettingDescriptor> Create()
    {
        return
        [
            new EnumSettingDescriptor(SettingsConstants.Language, Loc(LocalizationConstants.Settings.Language))
            {
                Description = Loc(LocalizationConstants.Settings.Language_Desc),
                IsRequired = static _ => true,
                DefaultValue = SettingsConstants.Auto,
                Options = BuildLanguageOptions(),
            },
        ];
    }

    private static IReadOnlyList<EnumOption> BuildLanguageOptions()
    {
        var options = new List<EnumOption>
        {
            new(SettingsConstants.Auto, Loc(LocalizationConstants.Settings.Language_Auto)),
        };

        try
        {
            foreach (var lang in OcrEngine.AvailableRecognizerLanguages
                         .OrderBy(l => l.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                var tag = lang.LanguageTag;
                var displayName = lang.DisplayName;
                options.Add(new EnumOption(tag, new LocalizedString(tag, _ => displayName)));
            }
        }
        catch
        {
            // WinRT may fail without package identity; fall back to the shared language list.
            foreach (var option in Languages.All)
                options.Add(option);
        }

        // Ensure at least auto + common languages when the OS reports none.
        if (options.Count == 1)
        {
            foreach (var option in Languages.All)
                options.Add(option);
        }

        return options;
    }

    private static LocalizedString Loc(string key)
        => new(key, culture => Properties.Resources.ResourceManager.GetString(key, culture)!);
}
