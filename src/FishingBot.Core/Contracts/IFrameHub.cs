namespace FishingBot.Core.Contracts;

public interface IFrameHub<TFrame> : IDisposable
    where TFrame : notnull
{
    void Start();

    void Stop();

    bool TryGetLatestFrame(out TFrame frame);
}
