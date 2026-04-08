using FishingBot.Core.Config;
using Xunit;

namespace FishingBot.Tests.Config;

public class NormalizedRectTests
{
    [Fact]
    public void ToPixelRect_ConvertsNormalizedToPixels_For2560x1440()
    {
        // Arrange
        var rect = new NormalizedRect(0.5, 0.25, 0.1, 0.2);

        // Act
        var result = rect.ToPixelRect(2560, 1440);

        // Assert
        Assert.Equal(1280, result.X);
        Assert.Equal(360, result.Y);
        Assert.Equal(256, result.Width);
        Assert.Equal(288, result.Height);
    }
}
