namespace FishingBot.Core.Fsm;

public enum FishingState
{
    Idle,
    WaitStartPrompt,
    StartFishing,
    WaitBite,
    Hook,
    Fight,
    CatchMenu,
    ApplyAction,
    Recast
}
