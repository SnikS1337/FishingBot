using FishingBot.Core.V2.Runtime.Policies;
using Xunit;

namespace FishingBot.Tests.V2.Policies;

public class SecondPressPolicyTests
{
    [Fact]
    public void Allow_ReturnsFalse_WhenCooldownNotElapsed()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var now = start.AddMilliseconds(1800);

        // Act
        var allowed = SecondPressPolicy.Allow(start, now, 2500);

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public void Allow_ReturnsTrue_WhenCooldownElapsed()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var now = start.AddMilliseconds(2600);

        // Act
        var allowed = SecondPressPolicy.Allow(start, now, 2500);

        // Assert
        Assert.True(allowed);
    }

    [Fact]
    public void Allow_ReturnsTrue_WhenElapsedEqualsCooldown()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var now = start.AddMilliseconds(2500);

        // Act
        var allowed = SecondPressPolicy.Allow(start, now, 2500);

        // Assert
        Assert.True(allowed);
    }

    [Fact]
    public void Allow_NormalizesNegativeCooldownToZero()
    {
        // Arrange
        var start = DateTimeOffset.UtcNow;
        var now = start;

        // Act
        var allowed = SecondPressPolicy.Allow(start, now, -100);

        // Assert
        Assert.True(allowed);
    }
}
