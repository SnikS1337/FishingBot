using FishingBot.Core.Fsm;
using Xunit;

namespace FishingBot.Tests.Fsm;

public class FishingStateMachineTests
{
    [Fact]
    public void FirstStartPromptDetected_MovesToEnterFishingMode()
    {
        // Arrange
        var fsm = new FishingStateMachine(FishingState.WaitStartPrompt);

        // Act
        fsm.Handle(FishingEvent.StartPromptDetected);

        // Assert
        Assert.Equal(FishingState.EnterFishingMode, fsm.Current);
    }

    [Fact]
    public void SecondStartPromptDetected_MovesToStartFishing()
    {
        // Arrange
        var fsm = new FishingStateMachine(FishingState.WaitSecondStartPrompt);

        // Act
        fsm.Handle(FishingEvent.StartPromptDetected);

        // Assert
        Assert.Equal(FishingState.StartFishing, fsm.Current);
    }

    [Fact]
    public void BiteDetected_MovesToHook_FromWaitBite()
    {
        // Arrange
        var fsm = new FishingStateMachine(FishingState.WaitBite);

        // Act
        fsm.Handle(FishingEvent.BiteDetected);

        // Assert
        Assert.Equal(FishingState.Hook, fsm.Current);
    }

    [Fact]
    public void Timeout_ResetsToWaitStartPrompt_FromAnyState()
    {
        // Arrange
        var fsm = new FishingStateMachine(FishingState.Fight);

        // Act
        fsm.Handle(FishingEvent.Timeout);

        // Assert
        Assert.Equal(FishingState.WaitStartPrompt, fsm.Current);
    }

    [Fact]
    public void FullHappyPath_CompletesCycleToWaitStartPrompt()
    {
        // Arrange
        var fsm = new FishingStateMachine(FishingState.WaitStartPrompt);

        // Act
        fsm.Handle(FishingEvent.StartPromptDetected);
        fsm.Handle(FishingEvent.StartFishingDone);
        fsm.Handle(FishingEvent.StartPromptDetected);
        fsm.Handle(FishingEvent.StartFishingDone);
        fsm.Handle(FishingEvent.BiteDetected);
        fsm.Handle(FishingEvent.HookDone);
        fsm.Handle(FishingEvent.CatchMenuDetected);
        fsm.Handle(FishingEvent.ActionApplied);
        fsm.Handle(FishingEvent.RecastDone);

        // Assert
        Assert.Equal(FishingState.WaitStartPrompt, fsm.Current);
    }
}
