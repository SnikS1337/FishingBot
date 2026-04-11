using FishingBot.Core.Vision;
using OpenCvSharp;
using Xunit;

namespace FishingBot.Tests.Vision;

public class FightDetectorTests
{
    [Fact]
    public void Detect_ReturnsTrue_WhenVisibleFightBarContainsMarkerAtExpectedX()
    {
        // Arrange
        var detector = new FightDetector();
        using var frame = CreateFrameWithBarAndMarker(
            frameWidth: 240,
            barStartX: 30,
            barEndX: 210,
            markerX: 132,
            markerColor: Scalar.White);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.InRange(result.MarkerX, 130, 134);
        Assert.InRange(result.Confidence, 0.68, 0.71);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenFrameIsNull()
    {
        // Arrange
        var detector = new FightDetector();

        // Act
        var result = detector.Detect(null!);

        // Assert
        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Confidence);
        Assert.Equal(-1, result.MarkerX);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenFrameIsEmpty()
    {
        // Arrange
        var detector = new FightDetector();
        using var frame = new Mat();

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Confidence);
        Assert.Equal(-1, result.MarkerX);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenNoValidFightBarOrMarkerExists()
    {
        // Arrange
        var detector = new FightDetector();
        using var frame = new Mat(60, 240, MatType.CV_8UC3, Scalar.Black);

        Cv2.Rectangle(frame, new Rect(20, 12, 36, 36), new Scalar(0, 220, 255), -1);
        Cv2.Rectangle(frame, new Rect(176, 20, 10, 10), Scalar.White, -1);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Confidence);
        Assert.Equal(-1, result.MarkerX);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenFightBarIsNarrowerThanMinimumWidthHeuristic()
    {
        // Arrange
        var detector = new FightDetector();
        using var frame = CreateFrameWithBarAndMarker(
            frameWidth: 240,
            barStartX: 70,
            barEndX: 128,
            markerX: 100,
            markerColor: Scalar.White);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Confidence);
        Assert.Equal(-1, result.MarkerX);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenMarkerBrightnessDoesNotExceedThreshold()
    {
        // Arrange
        var detector = new FightDetector();
        using var frame = CreateFrameWithBarAndMarker(
            frameWidth: 240,
            barStartX: 30,
            barEndX: 210,
            markerX: 132,
            markerColor: new Scalar(225, 225, 225));

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Confidence);
        Assert.Equal(-1, result.MarkerX);
    }

    private static Mat CreateFrameWithBarAndMarker(int frameWidth, int barStartX, int barEndX, int markerX, Scalar markerColor)
    {
        var frame = new Mat(60, frameWidth, MatType.CV_8UC3, new Scalar(20, 20, 20));

        Cv2.Line(frame, new Point(barStartX, 30), new Point(barEndX, 30), new Scalar(0, 220, 255), 2);
        Cv2.Line(frame, new Point(markerX, 18), new Point(markerX, 42), markerColor, 3);

        return frame;
    }
}
