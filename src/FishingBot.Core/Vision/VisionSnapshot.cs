namespace FishingBot.Core.Vision;

public sealed record VisionSnapshot(
    bool StartPromptDetected,
    double StartPromptPrimaryConfidence,
    double StartPromptAltConfidence,
    double StartPromptConfidence,
    bool AimAligned,
    int AimMarkerX,
    double AimConfidence,
    bool BiteDetected,
    double BiteConfidence,
    bool FightDetected,
    double FightConfidence,
    int FightMarkerX,
    bool CatchMenuDetected,
    double CatchMenuConfidence);
