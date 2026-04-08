namespace FishingBot.Core.Fsm;

public enum FishingEvent
{
    StartPromptDetected,
    StartFishingDone,
    BiteDetected,
    HookDone,
    FightDetected,
    CatchMenuDetected,
    ActionApplied,
    RecastDone,
    Timeout,
    Reset
}
