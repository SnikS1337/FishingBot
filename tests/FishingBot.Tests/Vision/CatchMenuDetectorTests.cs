using FishingBot.Core.Vision;
using OpenCvSharp;
using Xunit;

namespace FishingBot.Tests.Vision;

public class CatchMenuDetectorTests
{
    [Fact]
    public void Detect_ReturnsFalse_OnBrightEmptyFrame()
    {
        // Arrange
        var detector = new CatchMenuDetector();
        using var frame = new Mat(200, 300, MatType.CV_8UC3, new Scalar(240, 240, 240));

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
    }
}
