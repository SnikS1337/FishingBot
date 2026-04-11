using FishingBot.Core.V2.Capture;
using OpenCvSharp;
using Xunit;

namespace FishingBot.Tests.V2.Runtime;

public class FrameHubSmokeTests
{
    [Fact]
    public void FrameSnapshot_ClonesSourceFrameData()
    {
        // Arrange
        using var source = new Mat(1, 1, MatType.CV_8UC3, new Scalar(10, 20, 30));
        using var snapshot = new FrameSnapshot(1, DateTimeOffset.UtcNow, source);

        // Act
        source.Set(0, 0, new Vec3b(1, 2, 3));
        using var frame = snapshot.Frame;
        var pixel = frame.At<Vec3b>(0, 0);

        // Assert
        Assert.Equal((byte)10, pixel.Item0);
        Assert.Equal((byte)20, pixel.Item1);
        Assert.Equal((byte)30, pixel.Item2);
    }

    [Fact]
    public void FrameSnapshot_UsesFrameDimensions()
    {
        // Arrange
        using var source = new Mat(3, 2, MatType.CV_8UC3);

        // Act
        using var snapshot = new FrameSnapshot(1, DateTimeOffset.UtcNow, source);

        // Assert
        Assert.Equal(2, snapshot.Width);
        Assert.Equal(3, snapshot.Height);
    }

    [Fact]
    public void TryGetLatestFrame_ReturnsFalse_WhenNoFrameHasBeenPublished()
    {
        // Arrange
        using var hub = new FrameHub();

        // Act
        var available = hub.TryGetLatestFrame(out var latest);

        // Assert
        Assert.False(available);
        Assert.Null(latest);
    }

    [Fact]
    public void PublishThenRead_ReturnsLatestFrameMetadata()
    {
        // Arrange
        using var hub = new FrameHub();
        using var frameMat = new Mat(1, 1, MatType.CV_8UC3);
        using var frame = new FrameSnapshot(123, DateTimeOffset.UtcNow, frameMat);

        // Act
        hub.Publish(frame);
        var available = hub.TryGetLatestFrame(out var latest);
        using var ownedLatest = latest;
        Assert.True(available);
        Assert.NotNull(ownedLatest);
        using var latestFrame = ownedLatest.Frame;

        // Assert
        Assert.Equal(123, ownedLatest.SequenceId);
        Assert.Equal(1, ownedLatest.Width);
        Assert.Equal(1, ownedLatest.Height);
        Assert.Equal(1, latestFrame.Cols);
        Assert.Equal(1, latestFrame.Rows);
    }

    [Fact]
    public void Publish_CapturesIndependentSnapshotOwnership()
    {
        // Arrange
        using var hub = new FrameHub();
        using var source = new Mat(1, 1, MatType.CV_8UC3, new Scalar(50, 60, 70));
        using var frame = new FrameSnapshot(123, DateTimeOffset.UtcNow, source);

        // Act
        hub.Publish(frame);
        var available = hub.TryGetLatestFrame(out var latest);
        using var ownedLatest = latest;
        Assert.True(available);
        Assert.NotNull(ownedLatest);
        using var latestFrame = ownedLatest.Frame;
        var pixel = latestFrame.At<Vec3b>(0, 0);

        // Assert
        Assert.Equal((byte)50, pixel.Item0);
        Assert.Equal((byte)60, pixel.Item1);
        Assert.Equal((byte)70, pixel.Item2);
    }

    [Fact]
    public void Publish_ReplacesPreviouslyPublishedFrame()
    {
        // Arrange
        using var hub = new FrameHub();
        using var firstMat = new Mat(1, 1, MatType.CV_8UC3);
        using var secondMat = new Mat(1, 1, MatType.CV_8UC3);
        using var first = new FrameSnapshot(1, DateTimeOffset.UtcNow, firstMat);
        using var second = new FrameSnapshot(2, DateTimeOffset.UtcNow.AddMilliseconds(5), secondMat);

        // Act
        hub.Publish(first);
        hub.Publish(second);
        var available = hub.TryGetLatestFrame(out var latest);
        using var ownedLatest = latest;

        // Assert
        Assert.True(available);
        Assert.NotNull(ownedLatest);
        Assert.Equal(2, ownedLatest.SequenceId);
        Assert.Equal(1, ownedLatest.Width);
        Assert.Equal(1, ownedLatest.Height);
    }

    [Fact]
    public void TryGetLatestFrame_TransfersSnapshotOwnershipToCaller()
    {
        // Arrange
        using var hub = new FrameHub();
        using var frameMat = new Mat(1, 1, MatType.CV_8UC3, new Scalar(11, 12, 13));
        using var frame = new FrameSnapshot(7, DateTimeOffset.UtcNow, frameMat);

        // Act
        hub.Publish(frame);
        var available = hub.TryGetLatestFrame(out var latest);
        using var ownedLatest = latest;
        Assert.True(available);
        Assert.NotNull(ownedLatest);
        using var latestFrame = ownedLatest.Frame;
        var pixel = latestFrame.At<Vec3b>(0, 0);

        // Assert
        Assert.Equal((byte)11, pixel.Item0);
        Assert.Equal((byte)12, pixel.Item1);
        Assert.Equal((byte)13, pixel.Item2);
    }

    [Fact]
    public void StartAndStop_DoNotPreventReadingPublishedFrame()
    {
        // Arrange
        using var hub = new FrameHub();
        using var frameMat = new Mat(1, 1, MatType.CV_8UC3);
        using var frame = new FrameSnapshot(9, DateTimeOffset.UtcNow, frameMat);
        hub.Start();
        hub.Publish(frame);
        hub.Stop();

        // Act
        var available = hub.TryGetLatestFrame(out var latest);
        using var ownedLatest = latest;

        // Assert
        Assert.True(available);
        Assert.NotNull(ownedLatest);
        Assert.Equal(9, ownedLatest.SequenceId);
    }

    [Fact]
    public void ReturnedSnapshot_RemainsUsable_AfterHubDispose()
    {
        // Arrange
        using var hub = new FrameHub();
        using var source = new Mat(1, 1, MatType.CV_8UC3, new Scalar(21, 22, 23));
        using var published = new FrameSnapshot(10, DateTimeOffset.UtcNow, source);
        hub.Publish(published);
        var available = hub.TryGetLatestFrame(out var latest);
        using var ownedLatest = latest;
        Assert.True(available);
        Assert.NotNull(ownedLatest);

        // Act
        hub.Dispose();
        using var latestFrame = ownedLatest.Frame;
        var pixel = latestFrame.At<Vec3b>(0, 0);

        // Assert
        Assert.Equal((byte)21, pixel.Item0);
        Assert.Equal((byte)22, pixel.Item1);
        Assert.Equal((byte)23, pixel.Item2);
    }

    [Fact]
    public void Start_ThrowsObjectDisposedException_AfterDispose()
    {
        // Arrange
        using var hub = new FrameHub();
        hub.Dispose();

        // Act
        var exception = Record.Exception(() => hub.Start());

        // Assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public void Stop_ThrowsObjectDisposedException_AfterDispose()
    {
        // Arrange
        using var hub = new FrameHub();
        hub.Dispose();

        // Act
        var exception = Record.Exception(() => hub.Stop());

        // Assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public void Publish_ThrowsObjectDisposedException_AfterDispose()
    {
        // Arrange
        using var hub = new FrameHub();
        using var frameMat = new Mat(1, 1, MatType.CV_8UC3);
        using var frame = new FrameSnapshot(5, DateTimeOffset.UtcNow, frameMat);
        hub.Dispose();

        // Act
        var exception = Record.Exception(() => hub.Publish(frame));

        // Assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public void TryGetLatestFrame_ThrowsObjectDisposedException_AfterDispose()
    {
        // Arrange
        using var hub = new FrameHub();
        hub.Dispose();

        // Act
        var exception = Record.Exception(() => hub.TryGetLatestFrame(out _));

        // Assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        using var hub = new FrameHub();

        // Act
        hub.Dispose();
        var exception = Record.Exception(hub.Dispose);

        // Assert
        Assert.Null(exception);
    }
}
