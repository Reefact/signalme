using SignalMe.Sessions;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     A session monitor the test drives by hand: it raises a change when told to, and can be made to
///     behave in the ways the real one does inside <see cref="Start" />.
/// </summary>
public sealed class FakeSessionMonitor : ISessionMonitor {

    public FakeSessionMonitor(SessionState current = SessionState.Active) {
        Current = current;
    }

    /// <inheritdoc />
    public SessionState Current { get; private set; }

    /// <inheritdoc />
    public event EventHandler<SessionState>? StateChanged;

    /// <summary>
    ///     What <see cref="Start" /> finds when it re-reads the session: when set and different, it flips
    ///     <see cref="Current" /> silently, or raises the change when <see cref="RaiseOnStart" /> is set,
    ///     like the real monitor closing the gap between construction and subscription.
    /// </summary>
    public SessionState? StateOnStart { get; set; }

    /// <summary>Raise <see cref="StateOnStart" /> from inside <see cref="Start" /> rather than flipping silently.</summary>
    public bool RaiseOnStart { get; set; }

    /// <summary>Thrown by <see cref="Start" />, like the real monitor failing to create its hidden window.</summary>
    public Exception? StartFailure { get; set; }

    public bool Started    { get; private set; }
    public bool IsDisposed { get; private set; }

    /// <summary>Reports a change, the way the OS would: <see cref="Current" /> first, then the event.</summary>
    public void Raise(SessionState state) {
        Current = state;
        StateChanged?.Invoke(this, state);
    }

    /// <inheritdoc />
    public void Start() {
        Started = true;
        if (StartFailure is not null) { throw StartFailure; }
        if (StateOnStart is not { } state) { return; }

        if (RaiseOnStart) {
            Raise(state);
        } else {
            Current = state;
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        IsDisposed = true;
    }

}
