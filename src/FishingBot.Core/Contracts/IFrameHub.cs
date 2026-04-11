using System.Diagnostics.CodeAnalysis;

namespace FishingBot.Core.Contracts;

public interface IFrameHub<TFrame> : IDisposable
    where TFrame : notnull
{
    void Start();

    void Stop();

    /// <summary>
    /// Attempts to read the latest published frame snapshot.
    /// </summary>
    /// <param name="frame">
    /// When this method returns <see langword="true" />, contains a caller-owned snapshot that must be disposed by the caller.
    /// When this method returns <see langword="false" />, contains <see langword="null" /> and no ownership is transferred.
    /// </param>
    bool TryGetLatestFrame([NotNullWhen(true)] out TFrame? frame);
}
