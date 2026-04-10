using FishingBot.Core.V2.Runtime.Policies;
using Xunit;

namespace FishingBot.Tests.V2.Policies;

public class CastReadinessPolicyTests
{
    [Fact]
    public void Allow_ReturnsTrue_WhenMarkerInsideGreenWindow()
    {
        // Arrange
        var input = new CastReadinessInput(
            CastBarVisible: true,
            GreenWindowVisible: true,
            WhiteMarkerVisible: true,
            MarkerInsideGreen: true);

        // Act
        var allowed = CastReadinessPolicy.Allow(input);

        // Assert
        Assert.True(allowed);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public void Allow_ReturnsFalse_WhenAnyRequiredSignalMissing(
        bool castBarVisible,
        bool greenWindowVisible,
        bool whiteMarkerVisible,
        bool markerInsideGreen)
    {
        // Arrange
        var input = new CastReadinessInput(
            CastBarVisible: castBarVisible,
            GreenWindowVisible: greenWindowVisible,
            WhiteMarkerVisible: whiteMarkerVisible,
            MarkerInsideGreen: markerInsideGreen);

        // Act
        var allowed = CastReadinessPolicy.Allow(input);

        // Assert
        Assert.False(allowed);
    }
}
