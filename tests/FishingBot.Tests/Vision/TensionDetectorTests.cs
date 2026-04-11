using FishingBot.Core.Vision;
using OpenCvSharp;
using Xunit;

namespace FishingBot.Tests.Vision;

public class TensionDetectorTests
{
    [Fact]
    public void Detect_ReturnsFalse_WhenFrameIsNull()
    {
        // Arrange
        var detector = new TensionDetector();

        // Act
        var result = detector.Detect(null!);

        // Assert
        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public void Detect_ReturnsTrue_WhenBottomBandContainsLocalizedRedSegment()
    {
        // Arrange
        var detector = new TensionDetector();
        using var frame = new Mat(120, 240, MatType.CV_8UC3, new Scalar(35, 35, 35));

        // Red appears only in part of the lower band, not across the full ROI.
        Cv2.Rectangle(frame, new Rect(130, 98, 70, 16), new Scalar(30, 40, 230), -1);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.True(result.Confidence > 0.20);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenBottomBandIsNeutralAndDark()
    {
        // Arrange
        var detector = new TensionDetector();
        using var frame = new Mat(120, 240, MatType.CV_8UC3, new Scalar(35, 35, 35));

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenBottomBandContainsScatteredRedNoiseWithoutLocalizedSignal()
    {
        // Arrange
        var detector = new TensionDetector();
        using var frame = new Mat(120, 240, MatType.CV_8UC3, new Scalar(35, 35, 35));

        for (var x = 0; x < frame.Cols; x += 6)
        {
            Cv2.Rectangle(frame, new Rect(x, 92, 2, 8), new Scalar(30, 40, 230), -1);
            Cv2.Rectangle(frame, new Rect((x + 3) % frame.Cols, 104, 2, 8), new Scalar(30, 40, 230), -1);
        }

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
    }
}
