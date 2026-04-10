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
    public void Detect_ReturnsTrue_OnEKeyLikePrompt()
    {
        // Arrange
        var detector = new StartPromptDetector();
        using var frame = new Mat(80, 220, MatType.CV_8UC3, new Scalar(40, 40, 40));

        // Имитация светлой кнопки E справа-снизу.
        Cv2.Rectangle(frame, new Rect(170, 45, 28, 28), new Scalar(245, 245, 245), -1);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.True(result.Confidence > 0.20);
    }
}
