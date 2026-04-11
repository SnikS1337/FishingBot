using OpenCvSharp;

namespace FishingBot.Core.V2.Capture;

/// <summary>
/// Immutable metadata plus an owned deep-cloned frame buffer.
/// </summary>
public sealed record class FrameSnapshot : IDisposable
{
    private readonly Mat _frame;

    public FrameSnapshot(long sequenceId, DateTimeOffset capturedAtUtc, Mat frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Empty())
        {
            throw new ArgumentException("Frame must not be empty.", nameof(frame));
        }

        SequenceId = sequenceId;
        CapturedAtUtc = capturedAtUtc;
        _frame = frame.Clone();
        Width = _frame.Cols;
        Height = _frame.Rows;
    }

    public long SequenceId { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public int Width { get; }

    public int Height { get; }

    /// <summary>
    /// Returns a caller-owned deep copy of the snapshot frame.
    /// </summary>
    public Mat Frame => _frame.Clone();

    public void Dispose()
    {
        _frame.Dispose();
    }
}
