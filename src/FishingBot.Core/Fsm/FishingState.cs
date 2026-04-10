namespace FishingBot.Core.Fsm;

public enum FishingState
{
    Idle,
    WaitStartPrompt,
    EnterFishingMode,
    WaitSecondStartPrompt,
    StartFishing,
    WaitBite,
    Hook,
    Fight,
    CatchMenu,
    ApplyAction,
    Recast
}
