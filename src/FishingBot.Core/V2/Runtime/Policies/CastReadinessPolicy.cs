namespace FishingBot.Core.V2.Runtime.Policies;

public readonly record struct CastReadinessInput(
    bool CastBarVisible,
    bool GreenWindowVisible,
    bool WhiteMarkerVisible,
    bool MarkerInsideGreen);

public static class CastReadinessPolicy
{
    public static bool Allow(CastReadinessInput input)
        => input.CastBarVisible
            && input.GreenWindowVisible
            && input.WhiteMarkerVisible
            && input.MarkerInsideGreen;
}
