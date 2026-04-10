namespace FishingBot.Core.Contracts;

public interface IDetectorWorker<TSignalData>
    where TSignalData : notnull
{
    string Name { get; }

    SignalResult<TSignalData> Detect();
}
