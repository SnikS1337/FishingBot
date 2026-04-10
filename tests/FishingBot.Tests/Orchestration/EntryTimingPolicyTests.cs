using FishingBot.Core.Orchestration;
using Xunit;

namespace FishingBot.Tests.Orchestration;

public class EntryTimingPolicyTests
{
    [Fact]
    public void AllowSecondStartPrompt_ReturnsFalse_BeforeCooldown()
    {
        // Arrange
        var firstPressAt = DateTimeOffset.UtcNow;
        var now = firstPressAt.AddMilliseconds(1500);

        // Act
        var allowed = EntryTimingPolicy.AllowSecondStartPrompt(firstPressAt, now, cooldownMs: 2000);

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public void AllowSecondStartPrompt_ReturnsTrue_AfterCooldown()
    {
        // Arrange
        var firstPressAt = DateTimeOffset.UtcNow;
        var now = firstPressAt.AddMilliseconds(2100);

        // Act
        var allowed = EntryTimingPolicy.AllowSecondStartPrompt(firstPressAt, now, cooldownMs: 2000);

        // Assert
        Assert.True(allowed);
    }
}
