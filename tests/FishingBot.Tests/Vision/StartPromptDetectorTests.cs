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

    [Fact]
    public void Detect_ReturnsTrue_OnGreenPromptMarker()
    {
        // Arrange
        var detector = new StartPromptDetector();
        using var frame = new Mat(60, 200, MatType.CV_8UC3, Scalar.Black);
        Cv2.Rectangle(frame, new Rect(12, 12, 18, 18), new Scalar(0, 220, 0), -1);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.True(result.Confidence > 0.15);
    }
}
