namespace FishingBot.Core.Orchestration;

public static class CastTimingPolicy
{
    public static bool ShouldForceCast(DateTimeOffset enteredUtc, DateTimeOffset nowUtc, int timeoutMs)
    {
        var safeTimeoutMs = Math.Max(1, timeoutMs);
        return (nowUtc - enteredUtc).TotalMilliseconds >= safeTimeoutMs;
    }
}
