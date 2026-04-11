using FishingBot.Core.V2.Vision;
using OpenCvSharp;
using Xunit;

namespace FishingBot.Tests.V2.Vision;

public class CastBarDetectorV2Tests
{
    [Fact]
    public void Detect_ReturnsMarkerInsideGreen_WhenWhiteMarkerOverlapsGreenWindow()
    {
        // Arrange
        var detector = new CastBarDetectorV2();
        using var frame = CreateFrameWithBar(
            greenWindow: new Rect(150, 30, 60, 18),
            whiteMarker: new Rect(175, 24, 6, 30));

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.GreenWindowVisible);
        Assert.True(result.Data.WhiteMarkerVisible);
        Assert.True(result.Data.MarkerInsideGreenWindow);
    }

    [Fact]
    public void Detect_ReturnsMarkerOutsideGreen_WhenWhiteMarkerDoesNotOverlapGreenWindow()
    {
        // Arrange
        var detector = new CastBarDetectorV2();
        using var frame = CreateFrameWithBar(
            greenWindow: new Rect(150, 30, 60, 18),
            whiteMarker: new Rect(265, 24, 6, 30));

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.GreenWindowVisible);
        Assert.True(result.Data.WhiteMarkerVisible);
        Assert.False(result.Data.MarkerInsideGreenWindow);
    }

    [Fact]
    public void Detect_ReturnsNotDetected_WhenDarkCastBarGeometryIsMissing()
    {
        // Arrange
        var detector = new CastBarDetectorV2();
        using var frame = new Mat(80, 400, MatType.CV_8UC3, new Scalar(30, 30, 30));
        Cv2.Rectangle(frame, new Rect(150, 30, 60, 18), new Scalar(40, 220, 40), -1);
        Cv2.Rectangle(frame, new Rect(175, 24, 6, 30), new Scalar(245, 245, 245), -1);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
        Assert.Null(result.Data);
    }

    [Fact]
    public void Detect_ReturnsDetectedWithoutGreenWindow_WhenGreenWindowIsMissing()
    {
        // Arrange
        var detector = new CastBarDetectorV2();
        using var frame = CreateFrameWithBar(
            greenWindow: null,
            whiteMarker: new Rect(175, 24, 6, 30));

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.NotNull(result.Data);
        Assert.False(result.Data!.GreenWindowVisible);
        Assert.True(result.Data.WhiteMarkerVisible);
        Assert.False(result.Data.MarkerInsideGreenWindow);
    }

    [Fact]
    public void Detect_ReturnsDetectedWithoutWhiteMarker_WhenWhiteMarkerIsMissing()
    {
        // Arrange
        var detector = new CastBarDetectorV2();
        using var frame = CreateFrameWithBar(
            greenWindow: new Rect(150, 30, 60, 18),
            whiteMarker: null);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.GreenWindowVisible);
        Assert.False(result.Data.WhiteMarkerVisible);
        Assert.False(result.Data.MarkerInsideGreenWindow);
    }

    [Fact]
    public void Detect_ReturnsMarkerOutsideGreen_WhenMarkerCenterMatchesRightEdge()
    {
        // Arrange
        var detector = new CastBarDetectorV2();
        using var frame = CreateFrameWithBar(
            greenWindow: new Rect(150, 30, 60, 18),
            whiteMarker: new Rect(207, 24, 6, 30));

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.True(result.IsDetected);
        Assert.NotNull(result.Data);
        Assert.False(result.Data!.MarkerInsideGreenWindow);
    }

    [Fact]
    public void Detect_ReturnsNotDetected_WhenDarkCastBarHeightExceedsGeometryCutoff()
    {
        // Arrange
        var detector = new CastBarDetectorV2();
        using var frame = new Mat(80, 400, MatType.CV_8UC3, new Scalar(30, 30, 30));
        Cv2.Rectangle(frame, new Rect(40, 24, 160, 29), new Scalar(25, 25, 25), -1);
        Cv2.Rectangle(frame, new Rect(92, 24, 32, 29), new Scalar(40, 220, 40), -1);
        Cv2.Rectangle(frame, new Rect(107, 18, 6, 41), new Scalar(245, 245, 245), -1);

        // Act
        var result = detector.Detect(frame);

        // Assert
        Assert.False(result.IsDetected);
        Assert.Null(result.Data);
    }

    private static Mat CreateFrameWithBar(Rect? greenWindow, Rect? whiteMarker)
    {
        var frame = new Mat(80, 400, MatType.CV_8UC3, new Scalar(30, 30, 30));
        Cv2.Rectangle(frame, new Rect(40, 30, 320, 18), new Scalar(25, 25, 25), -1);

        if (greenWindow is Rect greenWindowRect)
        {
            Cv2.Rectangle(frame, greenWindowRect, new Scalar(40, 220, 40), -1);
        }

        if (whiteMarker is Rect whiteMarkerRect)
        {
            Cv2.Rectangle(frame, whiteMarkerRect, new Scalar(245, 245, 245), -1);
        }

        return frame;
    }
}
