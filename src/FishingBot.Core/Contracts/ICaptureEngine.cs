using FishingBot.Core.Capture;

namespace FishingBot.Core.Contracts;

public interface ICaptureEngine : IDisposable
{
    bool TryGetLatestFrame(out CapturedFrame frame);
}
