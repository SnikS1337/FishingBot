namespace FishingBot.Core.Logging;

public sealed record LogEntry(
    System.DateTimeOffset TimestampUtc,
    string Level,
    string Event,
    string Message);
