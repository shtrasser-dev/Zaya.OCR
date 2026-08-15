using Zaya.OCR.Impl.ProximityTextLayout;

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
        Assert.Contains(settings, s => s.Key == "angleToleranceDegrees");
        Assert.Contains(settings, s => s.Key == "lineSpacingThreshold");
        Assert.Contains(settings, s => s.Key == "lineOverhangTolerancePercent");
        Assert.Contains(settings, s => s.Key == "verticalColumns");
        Assert.Contains(settings, s => s.Key == "fontSizeTolerance");
        Assert.Contains(settings, s => s.Key == "wordFilters");
        Assert.Contains(settings, s => s.Key == "lineFilters");
        Assert.Contains(settings, s => s.Key == "paragraphFilters");
        Assert.Contains(settings, s => s.Key == "enableStabilization");
        Assert.Contains(settings, s => s.Key == "holdNewBlocks");
        Assert.Contains(settings, s => s.Key == "centerThresholdXPercent");
        Assert.Contains(settings, s => s.Key == "centerThresholdYPercent");
        Assert.Contains(settings, s => s.Key == "levenshteinThreshold");
        Assert.Contains(settings, s => s.Key == "ghostMaxFrames");
        Assert.Contains(settings, s => s.Key == "paragraphMergeHysteresisPercent");
        Assert.Contains(settings, s => s.Key == "sameLineWordGapHysteresisPercent");
        Assert.Equal(18, settings.Count);
    }

    [Fact]
    public void Settings_Defaults_AreCorrect()
    {
        using var service = new ProximityTextLayoutService();
        var settings = service.Settings.ToDictionary(s => s.Key);

        Assert.Equal(50, ((Zaya.Primitives.IntegerSettingDescriptor)settings["wordGapThreshold"]).DefaultValue);
        Assert.Equal(50, ((Zaya.Primitives.IntegerSettingDescriptor)settings["baselineDriftTolerance"]).DefaultValue);
        Assert.Equal(10, ((Zaya.Primitives.IntegerSettingDescriptor)settings["angleToleranceDegrees"]).DefaultValue);
        Assert.Equal(150, ((Zaya.Primitives.IntegerSettingDescriptor)settings["lineSpacingThreshold"]).DefaultValue);
        Assert.Equal(100, ((Zaya.Primitives.IntegerSettingDescriptor)settings["lineOverhangTolerancePercent"]).DefaultValue);
        Assert.False(((Zaya.Primitives.BooleanSettingDescriptor)settings["verticalColumns"]).DefaultValue);
        Assert.Equal(50, ((Zaya.Primitives.IntegerSettingDescriptor)settings["fontSizeTolerance"]).DefaultValue);
        Assert.True(((Zaya.Primitives.BooleanSettingDescriptor)settings["enableStabilization"]).DefaultValue);
        Assert.False(((Zaya.Primitives.BooleanSettingDescriptor)settings["holdNewBlocks"]).DefaultValue);
        Assert.Equal(300, ((Zaya.Primitives.IntegerSettingDescriptor)settings["centerThresholdXPercent"]).DefaultValue);
        Assert.Equal(75, ((Zaya.Primitives.IntegerSettingDescriptor)settings["centerThresholdYPercent"]).DefaultValue);
        Assert.Equal(8, ((Zaya.Primitives.IntegerSettingDescriptor)settings["levenshteinThreshold"]).DefaultValue);
        Assert.Equal(3, ((Zaya.Primitives.IntegerSettingDescriptor)settings["ghostMaxFrames"]).DefaultValue);
        Assert.Equal(120, ((Zaya.Primitives.IntegerSettingDescriptor)settings["paragraphMergeHysteresisPercent"]).DefaultValue);
        Assert.Equal(600, ((Zaya.Primitives.IntegerSettingDescriptor)settings["sameLineWordGapHysteresisPercent"]).DefaultValue);
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
            ["lineOverhangTolerancePercent"] = 150,
        };
        using var session = await service.CreateSessionAsync(settings, TestContext.Current.CancellationToken);
        Assert.NotNull(session);
    }
}
