namespace FishingBot.Core.Contracts;

public sealed record SignalResult<TData>
    where TData : notnull
{
    private SignalResult(bool isDetected, double confidence, TData? data)
    {
        if (double.IsNaN(confidence) || confidence < 0 || confidence > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");
        }

        if (isDetected && data is null)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Detected signals must include typed data.");
        }

        if (!isDetected && data is not null)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "Non-detected signals cannot include typed data.");
        }

        IsDetected = isDetected;
        Confidence = isDetected ? confidence : 0;
        Data = data;
    }

    public bool IsDetected { get; }

    public double Confidence { get; }

    public TData? Data { get; }

    public static SignalResult<TData> Detected(double confidence, TData data) => new(true, confidence, data);

    public static SignalResult<TData> NotDetected() => new(false, 0, default);
}
