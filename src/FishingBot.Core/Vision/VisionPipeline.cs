using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed class VisionPipeline
{
    private readonly StartPromptDetector _startPromptDetector;
    private readonly TensionDetector _tensionDetector;
    private readonly FightDetector _fightDetector;
    private readonly CatchMenuDetector _catchMenuDetector;

    public VisionPipeline(
        StartPromptDetector startPromptDetector,
        TensionDetector tensionDetector,
        FightDetector fightDetector,
        CatchMenuDetector catchMenuDetector)
    {
        _startPromptDetector = startPromptDetector;
        _tensionDetector = tensionDetector;
        _fightDetector = fightDetector;
        _catchMenuDetector = catchMenuDetector;
    }

    public VisionSnapshot Analyze(Mat startPromptRoi, Mat tensionRoi, Mat fightRoi, Mat catchMenuRoi)
    {
        var start = _startPromptDetector.Detect(startPromptRoi);
        var bite = _tensionDetector.Detect(tensionRoi);
        var fight = _fightDetector.Detect(fightRoi);
        var menu = _catchMenuDetector.Detect(catchMenuRoi);

        return new VisionSnapshot(
            StartPromptDetected: start.IsDetected,
            StartPromptConfidence: start.Confidence,
            BiteDetected: bite.IsDetected,
            BiteConfidence: bite.Confidence,
            FightDetected: fight.IsDetected,
            FightMarkerX: fight.MarkerX,
            CatchMenuDetected: menu.IsDetected,
            CatchMenuConfidence: menu.Confidence);
    }
}
