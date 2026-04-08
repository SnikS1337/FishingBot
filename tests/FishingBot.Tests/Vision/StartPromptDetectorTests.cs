using FishingBot.Core.Vision;
using OpenCvSharp;
using Xunit;

namespace FishingBot.Tests.Vision;

public class StartPromptDetectorTests
{
    [Fact]
    public void Detect_ReturnsFalse_OnEmptyFrame()
    {
        // Arrange
        var detector = new StartPromptDetector();
        using var frame = new Mat(100, 200, MatType.CV_8UC3, Scalar.Black);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
    }
}
