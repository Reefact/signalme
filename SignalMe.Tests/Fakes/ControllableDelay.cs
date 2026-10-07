using SignalMe.Infrastructure;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     Returns at once from every wait, like <see cref="InstantDelay" />, except the waits a test asked to
///     hold: those stay pending until released or cancelled.
/// </summary>
/// <remarks>
///     With instant waits an animation runs to its end inside the call that starts it, so nothing can
///     arrive "during" it. Holding one wait is how a test gets an animation that is genuinely running when
///     a lock, another intent or the shutdown comes in.
/// </remarks>
public sealed class ControllableDelay : IDelay {

    private readonly object                _lock  = new();
    private readonly Dictionary<int, Hold> _holds = new();

    /// <summary>How many waits the animations have asked for so far.</summary>
    public int Waits { get; private set; }

    /// <summary>Called on each wait with its 1-based number, before the cancellation check.</summary>
    public Action<int>? OnWait { get; set; }

    /// <summary>Makes the wait number <paramref name="wait" /> (1-based) pend until released or cancelled.</summary>
    public Hold HoldAt(int wait) {
        Hold hold = new();
        lock (_lock) { _holds[wait] = hold; }

        return hold;
    }

    /// <inheritdoc />
    public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken) {
        int   number;
        Hold? hold;
        lock (_lock) {
            number = ++Waits;
            _holds.TryGetValue(number, out hold);
        }

        OnWait?.Invoke(number);
        // Honoured exactly like Task.Delay would, so cancellation behaves the same as in production.
        cancellationToken.ThrowIfCancellationRequested();

        return hold is null ? Task.CompletedTask : hold.WaitAsync(cancellationToken);
    }

    /// <summary>A wait the test controls.</summary>
    public sealed class Hold {

        private readonly TaskCompletionSource _reached  = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completes once the animation has entered the held wait. Bounded, so a bug fails rather than hangs.</summary>
        public Task Reached => _reached.Task.WaitAsync(TimeSpan.FromSeconds(10));

        /// <summary>Lets the animation go on.</summary>
        public void Release() {
            _released.TrySetResult();
        }

        internal async Task WaitAsync(CancellationToken cancellationToken) {
            _reached.TrySetResult();
            await _released.Task.WaitAsync(cancellationToken);
        }

    }

}
