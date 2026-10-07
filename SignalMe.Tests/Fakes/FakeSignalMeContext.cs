using SignalMe.Modes;
using SignalMe.Runtime;
using SignalMe.Sessions;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     A context that records the intents a mode sends and answers them as told, so that a mode is tested
///     without a coordinator, a device or a loop behind it.
/// </summary>
public sealed class FakeSignalMeContext : ISignalMeContext {

    private readonly List<SignalMeIntent> _intents = new();

    private readonly TaskCompletionSource _intentReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released       = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc />
    public string ModeName { get; init; } = "manual";

    /// <inheritdoc />
    /// <remarks>Settable: the test decides what the mode sees when it asks.</remarks>
    public SignalMeState State { get; set; } = new(null, SessionState.Active, EffectiveStatus.Off, null);

    /// <summary>Every intent received, in order.</summary>
    public IReadOnlyList<SignalMeIntent> Intents => _intents;

    /// <summary>
    ///     What <see cref="ExecuteAsync" /> answers. By default what a coordinator answers when nothing gets
    ///     in the way: applied for a status or a turn-off, completed for a signal.
    /// </summary>
    public Func<SignalMeIntent, IntentOutcome> Outcome { get; set; } = intent => intent is SignalMeIntent.PlaySignal ? IntentOutcome.SignalCompleted : IntentOutcome.Applied;

    /// <summary>
    ///     Thrown by <see cref="ExecuteAsync" /> instead of answering, like a coordinator that died of the
    ///     device and rethrows its failure to whoever asked.
    /// </summary>
    public Exception? Failure { get; set; }

    /// <summary>
    ///     When set, an intent stays pending until <see cref="Release" />, so a test can look at what the
    ///     mode does, or does not do, while the coordinator is still working on it.
    /// </summary>
    public bool HoldsIntents { get; init; }

    /// <summary>Completes once the first intent has reached the context. Bounded, so a bug fails rather than hangs.</summary>
    public Task IntentReceived => _intentReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));

    /// <summary>Lets the held intent, and every later one, be answered.</summary>
    public void Release() {
        _released.TrySetResult();
    }

    /// <inheritdoc />
    public async Task<IntentOutcome> ExecuteAsync(SignalMeIntent intent, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(intent);
        cancellationToken.ThrowIfCancellationRequested();

        _intents.Add(intent);
        _intentReceived.TrySetResult();

        // Honoured like the real context's wait on the coordinator, so cancelling while an intent is
        // pending behaves the same as in production.
        if (HoldsIntents) { await _released.Task.WaitAsync(cancellationToken); }
        if (Failure is not null) { throw Failure; }

        return Outcome(intent);
    }

}
