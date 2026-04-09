namespace FishingBot.Core.Vision;

public sealed record VisionSnapshot(
    bool StartPromptDetected,
    double StartPromptConfidence,
    bool AimAligned,
    double AimConfidence,
    bool BiteDetected,
    double BiteConfidence,
    bool FightDetected,
    int FightMarkerX,
    bool CatchMenuDetected,
    double CatchMenuConfidence);
