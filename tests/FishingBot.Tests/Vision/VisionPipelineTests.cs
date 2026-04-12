using FishingBot.Core.Vision;
using OpenCvSharp;
using Xunit;

namespace FishingBot.Tests.Vision;

public class VisionPipelineTests
{
    [Theory]
    [InlineData("start")]
    [InlineData("aim")]
    [InlineData("tension")]
    [InlineData("fight")]
    [InlineData("menu")]
    public void Constructor_ThrowsArgumentNullException_WhenDependencyIsNull(string nullDependency)
    {
        // Arrange / Act
        var action = () => CreatePipeline(nullDependency, startPromptThreshold: 0.68);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.False(string.IsNullOrWhiteSpace(exception.ParamName));
    }

    [Fact]
    public void Analyze_UsesAlternateStartPromptAndMapsDetectorOutputsToStableSnapshotFields()
    {
        // Arrange
        using var startPromptRoi = new Mat(80, 220, MatType.CV_8UC3, Scalar.Black);
        using var startPromptAltRoi = CreatePromptFrame();
        using var aimRoi = CreateAimFrame(markerInsideZone: true);
        using var tensionRoi = CreateTensionFrame(withLocalizedRedSignal: true);
        using var fightRoi = CreateFightFrame(markerColor: Scalar.White);
        using var catchMenuRoi = new Mat(120, 200, MatType.CV_8UC3, new Scalar(10, 10, 10));

        var startDetector = new StartPromptDetector();
        var aimDetector = new AimDetector();
        var tensionDetector = new TensionDetector();
        var fightDetector = new FightDetector();
        var catchMenuDetector = new CatchMenuDetector();

        var start = startDetector.Detect(startPromptRoi);
        var startAlt = startDetector.Detect(startPromptAltRoi);
        var aim = aimDetector.Detect(aimRoi);
        var bite = tensionDetector.Detect(tensionRoi);
        var fight = fightDetector.Detect(fightRoi);
        var menu = catchMenuDetector.Detect(catchMenuRoi);

        Assert.True(startAlt.Confidence > start.Confidence);

        var threshold = (start.Confidence + startAlt.Confidence) / 2.0;
        var pipeline = new VisionPipeline(
            startDetector,
            aimDetector,
            tensionDetector,
            fightDetector,
            catchMenuDetector,
            threshold);

        // Act
        var snapshot = pipeline.Analyze(
            startPromptRoi,
            startPromptAltRoi,
            aimRoi,
            tensionRoi,
            fightRoi,
            catchMenuRoi);

        // Assert
        Assert.True(snapshot.StartPromptDetected);
        Assert.Equal(start.Confidence, snapshot.StartPromptPrimaryConfidence, precision: 6);
        Assert.Equal(startAlt.Confidence, snapshot.StartPromptAltConfidence, precision: 6);
        Assert.Equal(Math.Max(start.Confidence, startAlt.Confidence), snapshot.StartPromptConfidence, precision: 6);
        Assert.Equal(aim.IsDetected, snapshot.AimAligned);
        Assert.Equal(aim.Confidence, snapshot.AimConfidence, precision: 6);
        Assert.Equal(aim.MarkerX, snapshot.AimMarkerX);
        Assert.Equal(bite.IsDetected, snapshot.BiteDetected);
        Assert.Equal(bite.Confidence, snapshot.BiteConfidence, precision: 6);
        Assert.Equal(fight.IsDetected, snapshot.FightDetected);
        Assert.Equal(fight.Confidence, snapshot.FightConfidence, precision: 6);
        Assert.Equal(fight.MarkerX, snapshot.FightMarkerX);
        Assert.Equal(menu.IsDetected, snapshot.CatchMenuDetected);
        Assert.Equal(menu.Confidence, snapshot.CatchMenuConfidence, precision: 6);
    }

    [Fact]
    public void Analyze_PreservesStartPromptConfidenceWhileApplyingThresholdAndKeepingAbsentSignalsOff()
    {
        // Arrange
        using var startPromptRoi = CreateLowConfidencePromptFrame();
        using var startPromptAltRoi = new Mat(80, 220, MatType.CV_8UC3, Scalar.Black);
        using var aimRoi = CreateAimFrame(markerInsideZone: false);
        using var tensionRoi = CreateTensionFrame(withLocalizedRedSignal: false);
        using var fightRoi = CreateFightFrame(markerColor: new Scalar(225, 225, 225));
        using var catchMenuRoi = new Mat(120, 200, MatType.CV_8UC3, new Scalar(240, 240, 240));

        var startDetector = new StartPromptDetector();
        var aimDetector = new AimDetector();
        var start = startDetector.Detect(startPromptRoi);
        var startAlt = startDetector.Detect(startPromptAltRoi);
        var aim = aimDetector.Detect(aimRoi);
        Assert.InRange(start.Confidence, 0.01, 0.99);

        var threshold = Math.Min(1.0, Math.Max(start.Confidence, startAlt.Confidence) + 0.05);
        Assert.True(threshold > start.Confidence);
        Assert.True(threshold > startAlt.Confidence);

        var pipeline = new VisionPipeline(
            startDetector,
            new AimDetector(),
            new TensionDetector(),
            new FightDetector(),
            new CatchMenuDetector(),
            threshold);

        // Act
        var snapshot = pipeline.Analyze(
            startPromptRoi,
            startPromptAltRoi,
            aimRoi,
            tensionRoi,
            fightRoi,
            catchMenuRoi);

        // Assert
        Assert.False(snapshot.StartPromptDetected);
        Assert.Equal(start.Confidence, snapshot.StartPromptPrimaryConfidence, precision: 6);
        Assert.Equal(startAlt.Confidence, snapshot.StartPromptAltConfidence, precision: 6);
        Assert.Equal(Math.Max(start.Confidence, startAlt.Confidence), snapshot.StartPromptConfidence, precision: 6);
        Assert.False(snapshot.AimAligned);
        Assert.Equal(aim.MarkerX, snapshot.AimMarkerX);
        Assert.False(snapshot.BiteDetected);
        Assert.Equal(0, snapshot.BiteConfidence);
        Assert.False(snapshot.FightDetected);
        Assert.Equal(0, snapshot.FightConfidence);
        Assert.Equal(-1, snapshot.FightMarkerX);
        Assert.False(snapshot.CatchMenuDetected);
        Assert.Equal(0, snapshot.CatchMenuConfidence);
    }

    private static VisionPipeline CreatePipeline(string nullDependency, double startPromptThreshold)
    {
        return new VisionPipeline(
            nullDependency == "start" ? null! : new StartPromptDetector(),
            nullDependency == "aim" ? null! : new AimDetector(),
            nullDependency == "tension" ? null! : new TensionDetector(),
            nullDependency == "fight" ? null! : new FightDetector(),
            nullDependency == "menu" ? null! : new CatchMenuDetector(),
            startPromptThreshold);
    }

    private static Mat CreatePromptFrame()
    {
        var frame = new Mat(90, 320, MatType.CV_8UC3, new Scalar(140, 140, 140));
        Cv2.Rectangle(frame, new Rect(210, 62, 108, 24), new Scalar(24, 24, 24), -1);
        Cv2.Rectangle(frame, new Rect(285, 58, 24, 24), new Scalar(245, 245, 245), -1);
        return frame;
    }

    private static Mat CreateLowConfidencePromptFrame()
    {
        var frame = new Mat(80, 220, MatType.CV_8UC3, new Scalar(80, 80, 80));
        Cv2.Rectangle(frame, new Rect(165, 46, 18, 18), new Scalar(220, 220, 220), -1);
        return frame;
    }

    private static Mat CreateAimFrame(bool markerInsideZone)
    {
        var frame = new Mat(40, 200, MatType.CV_8UC3, Scalar.Black);
        Cv2.Rectangle(frame, new Rect(40, 10, 120, 20), new Scalar(25, 25, 25), -1);
        Cv2.Rectangle(frame, new Rect(70, 10, 60, 20), new Scalar(0, 255, 0), -1);

        var markerX = markerInsideZone ? 95 : 50;
        Cv2.Rectangle(frame, new Rect(markerX, 0, 8, 40), new Scalar(255, 255, 255), -1);
        return frame;
    }

    private static Mat CreateTensionFrame(bool withLocalizedRedSignal)
    {
        var frame = new Mat(120, 240, MatType.CV_8UC3, new Scalar(35, 35, 35));
        if (withLocalizedRedSignal)
        {
            Cv2.Rectangle(frame, new Rect(130, 98, 70, 16), new Scalar(30, 40, 230), -1);
        }

        return frame;
    }

    private static Mat CreateFightFrame(Scalar markerColor)
    {
        var frame = new Mat(60, 240, MatType.CV_8UC3, new Scalar(20, 20, 20));
        Cv2.Line(frame, new Point(30, 30), new Point(210, 30), new Scalar(0, 220, 255), 2);
        Cv2.Line(frame, new Point(132, 18), new Point(132, 42), markerColor, 3);
        return frame;
    }
}
