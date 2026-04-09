using System.Collections.Concurrent;
using System;
using FishingBot.Core.Contracts;

namespace FishingBot.Core.Logging;

public sealed class InMemoryLogSink : ILogSink
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly int _maxEntries;

    public InMemoryLogSink(int maxEntries = 500)
    {
        _maxEntries = Math.Max(50, maxEntries);
    }

    public event Action<LogEntry>? EntryWritten;

    public void Write(LogEntry entry)
    {
        _entries.Enqueue(entry);

        while (_entries.Count > _maxEntries)
        {
            _ = _entries.TryDequeue(out _);
        }

        EntryWritten?.Invoke(entry);
    }

    public IReadOnlyCollection<LogEntry> Snapshot()
    {
        return _entries.ToArray();
    }
}
