using FishingBot.Core.Vision;
using OpenCvSharp;
using Xunit;

namespace FishingBot.Tests.Vision;

public class TensionDetectorTests
{
    [Fact]
    public void Detect_ReturnsTrue_ForRedBarLikeFrame()
    {
        // Arrange
        var detector = new TensionDetector();
        using var frame = new Mat(20, 120, MatType.CV_8UC3, new Scalar(30, 30, 210));

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.True(result.Confidence > 0.5);
    }
}
