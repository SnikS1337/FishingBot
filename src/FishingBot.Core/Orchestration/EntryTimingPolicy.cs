namespace FishingBot.Core.Orchestration;

public static class EntryTimingPolicy
{
    public static bool AllowSecondStartPrompt(DateTimeOffset firstPressAtUtc, DateTimeOffset nowUtc, int cooldownMs)
    {
        var safeCooldownMs = Math.Max(0, cooldownMs);
        return (nowUtc - firstPressAtUtc).TotalMilliseconds >= safeCooldownMs;
    }
}
