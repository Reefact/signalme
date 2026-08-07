using SignalMe.Infrastructure;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     Returns from every wait immediately, so the animation tests run in milliseconds instead of sitting
///     through the real timings.
/// </summary>
public sealed class InstantDelay : IDelay {

    /// <summary>How many waits the animation has asked for so far.</summary>
    public int Waits { get; private set; }

    /// <summary>Called on each wait with its 1-based number, which lets a test cancel partway through.</summary>
    public Action<int>? OnWait { get; set; }

    /// <inheritdoc />
    public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken) {
        Waits++;
        OnWait?.Invoke(Waits);
        // Honoured exactly like Task.Delay would, so cancellation behaves the same as in production.
        cancellationToken.ThrowIfCancellationRequested();

        return Task.CompletedTask;
    }

}
