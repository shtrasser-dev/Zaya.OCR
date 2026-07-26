using Zaya.OCR.Impl.ProximityTextLayout.Services;

namespace Zaya.OCR.Impl.ProximityTextLayout.Tests;

public sealed class ProximityTextLayoutServiceTests
{
    [Fact]
    public void EngineId_ReturnsExpectedValue()
    {
        using var service = new ProximityTextLayoutService();
        Assert.Equal("proximity-text-layout", service.EngineId);
    }

    [Fact]
    public void DisplayName_IsNotEmpty()
    {
        using var service = new ProximityTextLayoutService();
        var name = service.DisplayName.GetValue(System.Globalization.CultureInfo.InvariantCulture);
        Assert.False(string.IsNullOrWhiteSpace(name));
    }

    [Fact]
    public void Settings_HasExpectedKeys()
    {
        using var service = new ProximityTextLayoutService();
        var settings = service.Settings;
        Assert.Contains(settings, s => s.Key == "wordGapThreshold");
        Assert.Contains(settings, s => s.Key == "baselineDriftTolerance");
        Assert.Contains(settings, s => s.Key == "lineSpacingThreshold");
        Assert.Contains(settings, s => s.Key == "leftEdgeAlignmentTolerance");
        Assert.Contains(settings, s => s.Key == "firstLineIndentTolerance");
        Assert.Contains(settings, s => s.Key == "enableCenterAlignment");
        Assert.Contains(settings, s => s.Key == "fontSizeTolerance");
        Assert.Equal(7, settings.Count);
    }

    [Fact]
    public void Settings_Defaults_AreCorrect()
    {
        using var service = new ProximityTextLayoutService();
        var settings = service.Settings.ToDictionary(s => s.Key);

        Assert.Equal(50, ((Zaya.Primitives.IntegerSettingDescriptor)settings["wordGapThreshold"]).DefaultValue);
        Assert.Equal(50, ((Zaya.Primitives.IntegerSettingDescriptor)settings["baselineDriftTolerance"]).DefaultValue);
        Assert.Equal(150, ((Zaya.Primitives.IntegerSettingDescriptor)settings["lineSpacingThreshold"]).DefaultValue);
        Assert.Equal(100, ((Zaya.Primitives.IntegerSettingDescriptor)settings["leftEdgeAlignmentTolerance"]).DefaultValue);
        Assert.Equal(300, ((Zaya.Primitives.IntegerSettingDescriptor)settings["firstLineIndentTolerance"]).DefaultValue);
        Assert.False(((Zaya.Primitives.BooleanSettingDescriptor)settings["enableCenterAlignment"]).DefaultValue);
        Assert.Equal(50, ((Zaya.Primitives.IntegerSettingDescriptor)settings["fontSizeTolerance"]).DefaultValue);
    }

    [Fact]
    public async Task CreateSession_NullSettings_Succeeds()
    {
        using var service = new ProximityTextLayoutService();
        using var session = await service.CreateSessionAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(session);
    }

    [Fact]
    public async Task CreateSession_WithSettings_Succeeds()
    {
        using var service = new ProximityTextLayoutService();
        var settings = new Dictionary<string, object>
        {
            ["wordGapThreshold"] = 80,
            ["enableCenterAlignment"] = true,
        };
        using var session = await service.CreateSessionAsync(settings, TestContext.Current.CancellationToken);
        Assert.NotNull(session);
    }
}
