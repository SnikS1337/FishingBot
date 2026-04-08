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
    }
}
