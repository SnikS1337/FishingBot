using FishingBot.Core.Logging;

namespace FishingBot.Core.Contracts;

public interface ILogSink
{
    void Write(LogEntry entry);
}
