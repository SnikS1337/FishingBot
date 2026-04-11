using FishingBot.Core.Vision;
using OpenCvSharp;
using Xunit;

namespace FishingBot.Tests.Vision;

public class AimDetectorTests
{
    [Fact]
    public void Detect_ReturnsTrue_WhenVisibleGreenZoneContainsWhiteMarker()
    {
        var detector = new AimDetector();
        using var frame = new Mat(40, 200, MatType.CV_8UC3, Scalar.Black);

        Cv2.Rectangle(frame, new Rect(40, 10, 120, 20), new Scalar(25, 25, 25), -1);
        Cv2.Rectangle(frame, new Rect(70, 10, 60, 20), new Scalar(0, 255, 0), -1);
        Cv2.Rectangle(frame, new Rect(95, 0, 8, 40), new Scalar(255, 255, 255), -1);

        var result = detector.Detect(frame);

        Assert.True(result.IsDetected);
        Assert.True(result.Confidence > 0.25);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenVisibleGreenZoneDoesNotContainWhiteMarker()
    {
        var detector = new AimDetector();
        using var frame = new Mat(40, 200, MatType.CV_8UC3, Scalar.Black);

        Cv2.Rectangle(frame, new Rect(40, 10, 120, 20), new Scalar(25, 25, 25), -1);
        Cv2.Rectangle(frame, new Rect(70, 10, 60, 20), new Scalar(0, 255, 0), -1);
        Cv2.Rectangle(frame, new Rect(50, 0, 8, 40), new Scalar(255, 255, 255), -1);

        var result = detector.Detect(frame);

        Assert.False(result.IsDetected);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenGreenZoneAndMarkerExistWithoutCastBarGeometry()
    {
        var detector = new AimDetector();
        using var frame = new Mat(40, 200, MatType.CV_8UC3, Scalar.Black);

        Cv2.Rectangle(frame, new Rect(70, 10, 60, 20), new Scalar(0, 255, 0), -1);
        Cv2.Rectangle(frame, new Rect(95, 0, 8, 40), new Scalar(255, 255, 255), -1);

        var result = detector.Detect(frame);

        Assert.False(result.IsDetected);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public void Detect_ReturnsFalse_WhenNoGreenZone()
    {
        var detector = new AimDetector();
        using var frame = new Mat(40, 200, MatType.CV_8UC3, Scalar.Black);

        Cv2.Rectangle(frame, new Rect(95, 0, 8, 40), new Scalar(255, 255, 255), -1);

        var result = detector.Detect(frame);

        Assert.False(result.IsDetected);
    }
}
