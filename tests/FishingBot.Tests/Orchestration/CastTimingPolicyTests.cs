using FishingBot.Core.Orchestration;
using Xunit;

namespace FishingBot.Tests.Orchestration;

public class CastTimingPolicyTests
{
    [Fact]
    public void ShouldForceCast_ReturnsFalse_BeforeTimeout()
    {
        // Arrange
        var entered = DateTimeOffset.UtcNow;
        var now = entered.AddMilliseconds(900);

        // Act
        var shouldForce = CastTimingPolicy.ShouldForceCast(entered, now, 1500);

        // Assert
        Assert.False(shouldForce);
    }

    [Fact]
    public void ShouldForceCast_ReturnsTrue_AtTimeout()
    {
        // Arrange
        var entered = DateTimeOffset.UtcNow;
        var now = entered.AddMilliseconds(1500);

        // Act
        var shouldForce = CastTimingPolicy.ShouldForceCast(entered, now, 1500);

        // Assert
        Assert.True(shouldForce);
    }
}
