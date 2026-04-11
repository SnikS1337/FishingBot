using FishingBot.Core.Contracts;
using OpenCvSharp;

namespace FishingBot.Core.Vision;

public sealed class VisionPipeline : IVisionPipeline
{
    private readonly double _startPromptThreshold;
    private readonly StartPromptDetector _startPromptDetector;
    private readonly AimDetector _aimDetector;
    private readonly TensionDetector _tensionDetector;
    private readonly FightDetector _fightDetector;
    private readonly CatchMenuDetector _catchMenuDetector;

    public VisionPipeline(
        StartPromptDetector startPromptDetector,
        AimDetector aimDetector,
        TensionDetector tensionDetector,
        FightDetector fightDetector,
        CatchMenuDetector catchMenuDetector,
        double startPromptThreshold = 0.68)
    {
        ArgumentNullException.ThrowIfNull(startPromptDetector);
        ArgumentNullException.ThrowIfNull(aimDetector);
        ArgumentNullException.ThrowIfNull(tensionDetector);
        ArgumentNullException.ThrowIfNull(fightDetector);
        ArgumentNullException.ThrowIfNull(catchMenuDetector);

        _startPromptThreshold = Math.Clamp(startPromptThreshold, 0.0, 1.0);
        _startPromptDetector = startPromptDetector;
        _aimDetector = aimDetector;
        _tensionDetector = tensionDetector;
        _fightDetector = fightDetector;
        _catchMenuDetector = catchMenuDetector;
    }

    public VisionSnapshot Analyze(
        Mat startPromptRoi,
        Mat startPromptAltRoi,
        Mat aimRoi,
        Mat tensionRoi,
        Mat fightRoi,
        Mat catchMenuRoi)
    {
        var start = _startPromptDetector.Detect(startPromptRoi);
        var startAlt = _startPromptDetector.Detect(startPromptAltRoi);

        // Детектируем любой из двух регионов
        var startConfidence = Math.Max(start.Confidence, startAlt.Confidence);
        var startDetected = startConfidence >= _startPromptThreshold;

        var aim = _aimDetector.Detect(aimRoi);
        var bite = _tensionDetector.Detect(tensionRoi);
        var fight = _fightDetector.Detect(fightRoi);
        var menu = _catchMenuDetector.Detect(catchMenuRoi);

        return new VisionSnapshot(
            StartPromptDetected: startDetected,
            StartPromptConfidence: startConfidence,
            AimAligned: aim.IsDetected,
            AimConfidence: aim.Confidence,
            BiteDetected: bite.IsDetected,
            BiteConfidence: bite.Confidence,
            FightDetected: fight.IsDetected,
            FightMarkerX: fight.MarkerX,
            CatchMenuDetected: menu.IsDetected,
            CatchMenuConfidence: menu.Confidence);
    }
}
