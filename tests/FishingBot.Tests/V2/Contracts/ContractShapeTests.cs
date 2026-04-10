using FishingBot.Core.Contracts;
using Xunit;

namespace FishingBot.Tests.V2.Contracts;

public class ContractShapeTests
{
    [Fact]
    public void IFrameHub_AllowsReadingLatestFrameThroughInterface()
    {
        // Arrange
        IFrameHub<string> hub = new FakeFrameHub("frame-1");

        // Act
        var available = hub.TryGetLatestFrame(out var frame);

        // Assert
        Assert.True(available);
        Assert.Equal("frame-1", frame);
    }

    [Fact]
    public void IDetectorWorker_AllowsTypedDetectorSpecificData()
    {
        // Arrange
        IDetectorWorker<FakeSignalData> worker = new FakeDetectorWorker();

        // Act
        var result = worker.Detect();

        // Assert
        Assert.Equal("fake", worker.Name);
        Assert.True(result.IsDetected);
        Assert.Equal(0.9, result.Confidence);
        Assert.Equal(new FakeSignalData(128), result.Data);
    }

    private sealed class FakeFrameHub(string latestFrame) : IFrameHub<string>
    {
        public void Dispose()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public bool TryGetLatestFrame(out string frame)
        {
            frame = latestFrame;
            return true;
        }
    }

    private sealed class FakeDetectorWorker : IDetectorWorker<FakeSignalData>
    {
        public string Name => "fake";

        public SignalResult<FakeSignalData> Detect() => SignalResult<FakeSignalData>.Detected(0.9, new FakeSignalData(128));
    }

    private sealed record FakeSignalData(int MarkerX);
}
