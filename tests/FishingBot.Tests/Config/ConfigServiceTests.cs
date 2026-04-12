using FishingBot.Core.Config;
using Xunit;

namespace FishingBot.Tests.Config;

public class ConfigServiceTests
{
    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        // Arrange
        var service = new ConfigService();
        var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");

        // Act
        var config = service.Load(path);

        // Assert
        Assert.NotNull(config);
        Assert.Equal("RELEASE", config.Fishing.Action);
        Assert.Equal(10, config.Detection.AimMinZonePixels);
        Assert.Equal(2, config.Detection.AimMinMarkerPixels);
        Assert.Equal(0.35, config.Detection.TensionSampleBandHeightRatio, 3);
        Assert.Equal(0.02, config.Detection.TensionRedRatioThreshold, 3);
        Assert.Equal(0.03, config.Detection.FightVisibleRatioThreshold, 3);
        Assert.Equal(225, config.Detection.FightMarkerBrightnessThreshold);
        Assert.Equal(1500, config.Telemetry.DetectOnlyLogIntervalMs);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsConfigValues()
    {
        // Arrange
        var service = new ConfigService();
        var path = Path.Combine(Path.GetTempPath(), $"cfg-{Guid.NewGuid():N}.json");
        var expected = new BotConfig
        {
            Fishing = new FishingConfig
            {
                Action = "TAKE",
                AutoRecast = true,
                RecastDelayMs = [1500, 2500]
            },
            Detection = new DetectionConfig
            {
                AimMinZonePixels = 18,
                AimMinMarkerPixels = 4,
                TensionSampleBandHeightRatio = 0.40,
                TensionRedRatioThreshold = 0.05,
                FightVisibleRatioThreshold = 0.08,
                FightMarkerBrightnessThreshold = 235
            },
            Telemetry = new TelemetryConfig
            {
                DetectOnlyLogIntervalMs = 2200
            },
            Regions = new RegionsConfig
            {
                StartPrompt = new NormalizedRect(0.02, 0.03, 0.30, 0.10)
            }
        };

        // Act
        service.Save(path, expected);
        var actual = service.Load(path);

        // Assert
        Assert.Equal("TAKE", actual.Fishing.Action);
        Assert.True(actual.Fishing.AutoRecast);
        Assert.Equal(1500, actual.Fishing.RecastDelayMs[0]);
        Assert.Equal(2500, actual.Fishing.RecastDelayMs[1]);
        Assert.Equal(0.02, actual.Regions.StartPrompt.X, 3);
        Assert.Equal(18, actual.Detection.AimMinZonePixels);
        Assert.Equal(4, actual.Detection.AimMinMarkerPixels);
        Assert.Equal(0.40, actual.Detection.TensionSampleBandHeightRatio, 3);
        Assert.Equal(0.05, actual.Detection.TensionRedRatioThreshold, 3);
        Assert.Equal(0.08, actual.Detection.FightVisibleRatioThreshold, 3);
        Assert.Equal(235, actual.Detection.FightMarkerBrightnessThreshold);
        Assert.Equal(2200, actual.Telemetry.DetectOnlyLogIntervalMs);
    }
}
