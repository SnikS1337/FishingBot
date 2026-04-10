namespace FishingBot.Core.Fsm;

public sealed class FishingStateMachine
{
    public FishingState Current { get; private set; }

    public FishingStateMachine(FishingState initial = FishingState.WaitStartPrompt)
    {
        Current = initial;
    }

    public void Handle(FishingEvent evt)
    {
        Current = (Current, evt) switch
        {
            (FishingState.WaitStartPrompt, FishingEvent.StartPromptDetected) => FishingState.EnterFishingMode,
            (FishingState.EnterFishingMode, FishingEvent.StartFishingDone) => FishingState.WaitSecondStartPrompt,
            (FishingState.WaitSecondStartPrompt, FishingEvent.StartPromptDetected) => FishingState.StartFishing,
            (FishingState.StartFishing, FishingEvent.StartFishingDone) => FishingState.WaitBite,
            (FishingState.WaitBite, FishingEvent.BiteDetected) => FishingState.Hook,
            (FishingState.Hook, FishingEvent.HookDone) => FishingState.Fight,
            (FishingState.Fight, FishingEvent.CatchMenuDetected) => FishingState.CatchMenu,
            (FishingState.CatchMenu, FishingEvent.ActionApplied) => FishingState.Recast,
            (FishingState.Recast, FishingEvent.RecastDone) => FishingState.WaitStartPrompt,
            (_, FishingEvent.Timeout) => FishingState.WaitStartPrompt,
            (_, FishingEvent.Reset) => FishingState.WaitStartPrompt,
            _ => Current
        };
    }
}
