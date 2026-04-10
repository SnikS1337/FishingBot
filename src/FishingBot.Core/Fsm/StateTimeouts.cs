namespace FishingBot.Core.Fsm;

public sealed class StateTimeouts
{
    public int WaitStartPromptMs { get; init; } = 30000;

    public int StartFishingMs { get; init; } = 12000;

    public int WaitBiteMs { get; init; } = 45000;

    public int FightMs { get; init; } = 30000;

    public int CatchMenuMs { get; init; } = 10000;
}
