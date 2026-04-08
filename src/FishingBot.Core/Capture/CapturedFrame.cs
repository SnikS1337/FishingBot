using OpenCvSharp;

namespace FishingBot.Core.Capture;

public sealed class CapturedFrame : IDisposable
{
    public CapturedFrame(Mat bgrFrame, DateTimeOffset capturedAtUtc)
    {
        BgrFrame = bgrFrame;
        CapturedAtUtc = capturedAtUtc;
    }

    public Mat BgrFrame { get; }

    public DateTimeOffset CapturedAtUtc { get; }

    public void Dispose()
    {
        BgrFrame.Dispose();
    }
}
