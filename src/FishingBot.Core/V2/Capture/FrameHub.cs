using System.Diagnostics.CodeAnalysis;
using FishingBot.Core.Contracts;

namespace FishingBot.Core.V2.Capture;

public sealed class FrameHub : IFrameHub<FrameSnapshot>
{
    private readonly object _sync = new();
    private bool _disposed;
    private FrameSnapshot? _latest;

    /// <summary>
    /// Starts the in-memory frame hub. Current implementation is a compatibility no-op.
    /// </summary>
    public void Start()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
        }
    }

    /// <summary>
    /// Stops the in-memory frame hub. Current implementation is a compatibility no-op.
    /// </summary>
    public void Stop()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
        }
    }

    public void Publish(FrameSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        FrameSnapshot? previous;

        lock (_sync)
        {
            ThrowIfDisposed();
            previous = _latest;
            _latest = CloneSnapshot(snapshot);
        }

        previous?.Dispose();
    }

    public bool TryGetLatestFrame([NotNullWhen(true)] out FrameSnapshot? frame)
    {
        lock (_sync)
        {
            ThrowIfDisposed();

            if (_latest is null)
            {
                frame = null;
                return false;
            }

            frame = CloneSnapshot(_latest);
            return true;
        }
    }

    public void Dispose()
    {
        FrameSnapshot? latest;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            latest = _latest;
            _latest = null;
        }

        latest?.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static FrameSnapshot CloneSnapshot(FrameSnapshot snapshot)
    {
        using var frame = snapshot.Frame;
        return new FrameSnapshot(snapshot.SequenceId, snapshot.CapturedAtUtc, frame);
    }
}
