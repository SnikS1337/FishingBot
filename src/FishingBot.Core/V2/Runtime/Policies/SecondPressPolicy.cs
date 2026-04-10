namespace FishingBot.Core.V2.Runtime.Policies;

public static class SecondPressPolicy
{
    public static bool Allow(DateTimeOffset firstActionUtc, DateTimeOffset nowUtc, int cooldownMs)
        => (nowUtc - firstActionUtc).TotalMilliseconds >= Math.Max(0, cooldownMs);
}
