#region Usings declarations

using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Converters;
using SignalMe.Infrastructure;
using SignalMe.Modes;
using SignalMe.MoodPatterns;
using SignalMe.Services;
using SignalMe.Sessions;

#endregion

namespace SignalMe.Runtime;

/// <summary>
///     The one owner of the device: decides what the LEDs show from the desired status, the session state
///     and the signal being played, and is the only component that ever writes to the device.
/// </summary>
/// <remarks>
///     <para>
///         The mode and the session monitor run on their own threads and could ask for the device at the
///         same moment. Rather than locks around every write, everything they say goes through one channel
///         and one loop (<see cref="RunAsync" />), which handles messages one at a time; animations are
///         child tasks of that loop, started and awaited by it, so a frame can never slip between two of its
///         writes. The initial render (<see cref="Initialize" />) runs before the loop and the final
///         turn-off (<see cref="TurnOffQuietly" />) after it: sequential, hence just as safe.
///     </para>
///     <para>
///         The loop awaits only the channel, never a <c>WhenAny</c> over the animation: with a single-reader
///         channel a second pending read would replace the first and lose its message. The animation
///         reports its end by posting a message of its own.
///     </para>
/// </remarks>
public sealed class StatusCoordinator : ISignalMeContext, IDisposable {

    #region Statics members declarations

    private static EffectiveStatus ComputeEffective(UserStatus? desired, SessionState session) {
        if (desired is null) { return EffectiveStatus.Off; }
        if (session == SessionState.Locked) { return EffectiveStatus.Away; }

        return desired switch {
            UserStatus.Available    => EffectiveStatus.Available,
            UserStatus.Busy         => EffectiveStatus.Busy,
            UserStatus.DoNotDisturb => EffectiveStatus.DoNotDisturb,
            _                       => throw new InvalidEnumArgumentException(nameof(desired), (int)desired, typeof(UserStatus))
        };
    }

    private static string Describe(UserStatus? status) {
        return status is null ? "off" : UserStatusConverter.ToCanonicalValue(status.Value);
    }

    /// <summary>
    ///     The error an animation died of, or null when it ran to its end or was merely cancelled: an
    ///     async method turns an <see cref="OperationCanceledException" /> into a cancelled task, but a
    ///     pattern could also surface one from inside an aggregate, and that is not a failure either.
    /// </summary>
    private static Exception? FailureOf(Task task) {
        if (!task.IsFaulted) { return null; }

        foreach (Exception exception in task.Exception!.Flatten().InnerExceptions) {
            if (exception is not OperationCanceledException) { return exception; }
        }

        return null;
    }

    /// <summary>Waits for a task to end, whichever way it ends; the caller then reads its status.</summary>
    private static async Task AwaitQuietlyAsync(Task task) {
        try {
            await task.ConfigureAwait(false);
        } catch (Exception) {
            // Examined through the task's status by the caller, which decides what it means.
        }
    }

    #endregion

    #region Fields declarations

    private readonly ILuxaforDevice    _device;
    private readonly UserCurrentStatus _store;
    private readonly IConsole          _console;
    private readonly IDelay            _delay;
    private readonly Channel<Message>  _channel;

    private UserStatus?    _desired;
    private SessionState   _session;
    private SignalMeState  _state;
    private RunningSignal? _running;
    private long           _lastSignalId;

    #endregion

    #region Constructors declarations

    public StatusCoordinator(string modeName, ILuxaforDevice device, UserCurrentStatus store, IConsole console, IDelay delay, UserStatus? initialDesiredStatus) {
        ArgumentNullException.ThrowIfNull(modeName);
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(delay);
        if (initialDesiredStatus == UserStatus.Away) { throw new ArgumentException("'away' is not a desired status: SignalMe shows it by itself while the session is locked.", nameof(initialDesiredStatus)); }

        ModeName = modeName;
        _device  = device;
        _store   = store;
        _console = console;
        _delay   = delay;
        // Created here rather than in RunAsync, so that a session event raised between the monitor's
        // start and the loop's start is queued instead of lost.
        _channel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        _desired = initialDesiredStatus;
        _session = SessionState.Active;
        _state   = new SignalMeState(_desired, _session, ComputeEffective(_desired, _session), null);
    }

    #endregion

    /// <inheritdoc />
    public string ModeName { get; }

    /// <inheritdoc />
    public SignalMeState State => Volatile.Read(ref _state);

    /// <summary>
    ///     Renders the status SignalMe starts with, synchronously, before the loop and the mode start.
    /// </summary>
    /// <remarks>
    ///     Nothing is persisted: the desired status comes from the store already, and the session state is
    ///     not something to remember. The runtime reads <paramref name="session" /> after the monitor was
    ///     started, so this render is never stale; a session event queued meanwhile that says the same
    ///     thing is dropped by the loop as a duplicate, so it is never doubled either.
    /// </remarks>
    /// <exception cref="DeviceCommandFailedException">The device refused the first render.</exception>
    public void Initialize(SessionState session) {
        Render(_desired, session);
        _session = session;
        PublishState();

        _console.WriteLine($"Status: {Describe(_desired)}");
        if (_desired is not null && _session == SessionState.Locked) { _console.WriteLine("Effective status: away"); }
    }

    /// <summary>
    ///     The processing loop. Completes normally when the token is cancelled; faults on a device or
    ///     unexpected error, after reporting it once. Never completes while an animation task is alive, and
    ///     on exit every pending or later <see cref="ExecuteAsync" /> is completed.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken) {
        Exception? fault = null;
        try {
            while (true) {
                Message message = await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                // A read that completed just before the stop came in: handled like a message found in the
                // drain, so that nothing is started once the runtime asked the loop to stop.
                if (cancellationToken.IsCancellationRequested) {
                    if (message is Message.Intent intent) { intent.Completion.TrySetCanceled(cancellationToken); }

                    throw new OperationCanceledException(cancellationToken);
                }
                await HandleAsync(message, cancellationToken).ConfigureAwait(false);
            }
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // The runtime asked the loop to stop: the normal way out.
        } catch (Exception exception) {
            fault = exception;
            _console.WriteError(ErrorReporting.Describe(exception));

            throw;
        } finally {
            await ShutdownAsync(fault, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Queues a session change. Thread-safe, never throws, ignored once the loop has stopped: it is
    ///     called from the monitor's thread, which must neither block nor fail.
    /// </summary>
    public void OnSessionChanged(SessionState state) {
        _channel.Writer.TryWrite(new Message.SessionChanged(state));
    }

    /// <summary>
    ///     Turns the device off, best effort: a refusal or an exception is reported on the error output and
    ///     never thrown, since this runs at shutdown where there is nothing left to do about it. The runtime
    ///     calls it only once the loop has completed (or when it never ran), which keeps the device to a
    ///     single writer.
    /// </summary>
    public void TurnOffQuietly() {
        _device.TurnOffQuietly(_console);
    }

    /// <inheritdoc />
    public async Task<IntentOutcome> ExecuteAsync(SignalMeIntent intent, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(intent);
        if (intent is SignalMeIntent.SetDesiredStatus { Status: UserStatus.Away }) { throw new ArgumentException("'away' is not a desired status: SignalMe shows it by itself while the session is locked.", nameof(intent)); }

        cancellationToken.ThrowIfCancellationRequested();

        // Continuations run asynchronously so that the mode's code never executes inside the loop,
        // between a completion and the loop's next device write.
        TaskCompletionSource<IntentOutcome> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_channel.Writer.TryWrite(new Message.Intent(intent, completion))) { throw new OperationCanceledException("SignalMe is stopping."); }

        return await completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    ///     A loop that ran has already settled everything in its shutdown; this disposes what a loop that
    ///     never ran could leave behind and makes sure a later intent is refused rather than queued forever.
    /// </remarks>
    public void Dispose() {
        _running?.Dispose();
        _running = null;

        _channel.Writer.TryComplete();
        while (_channel.Reader.TryRead(out Message? message)) {
            if (message is Message.Intent intent) { intent.Completion.TrySetCanceled(); }
        }
    }

    private async Task HandleAsync(Message message, CancellationToken cancellationToken) {
        try {
            switch (message) {
                case Message.SignalEnded ended:
                    // Only the signal still in the loop's hands is of interest: an interrupted one was
                    // settled when it was interrupted, and its message arrives late.
                    if (_running is not null && _running.Id == ended.SignalId) { await SettleRunningSignalAsync().ConfigureAwait(false); }

                    return;
                case Message.SessionChanged changed when changed.State == _session:
                    // A duplicate carries no information, and in particular must not interrupt a signal.
                    return;
            }

            // Anything else takes the device over: a running signal is stopped first and awaited, so that
            // no frame of it can land after the write that follows.
            if (_running is not null) { await InterruptRunningSignalAsync().ConfigureAwait(false); }

            switch (message) {
                case Message.Intent intent:
                    HandleIntent(intent, cancellationToken);

                    break;
                case Message.SessionChanged changed:
                    HandleSessionChanged(changed.State);

                    break;
            }
        } catch (Exception exception) {
            // Whoever sent the intent being handled must see why it did not happen, whether its own write
            // failed or the signal it interrupted turned out to have died: the message is out of the
            // channel already, so the shutdown drain would never reach it. The loop then dies of it.
            if (message is Message.Intent intent) { intent.Completion.TrySetException(exception); }

            throw;
        }
    }

    private void HandleIntent(Message.Intent message, CancellationToken cancellationToken) {
        switch (message.Value) {
            case SignalMeIntent.SetDesiredStatus set:
                ApplyDesiredStatus(set.Status);
                message.Completion.TrySetResult(IntentOutcome.Applied);

                break;
            case SignalMeIntent.TurnOff:
                ApplyTurnOff();
                message.Completion.TrySetResult(IntentOutcome.Applied);

                break;
            case SignalMeIntent.PlaySignal play:
                StartSignal(play.Mood, message.Completion, cancellationToken);

                break;
            default:
                throw new ArgumentException($"Unknown intent '{message.Value.GetType().Name}'.");
        }
    }

    private void ApplyDesiredStatus(UserStatus status) {
        // In-memory state and the store change only once the device obeyed: a refused status is neither
        // remembered nor reported as applied (spec §36).
        Render(status, _session);
        _desired = status;
        _store.Set(status);
        PublishState();

        _console.WriteLine($"Status: {Describe(status)}");
        if (_session == SessionState.Locked) { _console.WriteLine("Effective status: away"); }
    }

    private void ApplyTurnOff() {
        Render(null, _session);
        _desired = null;
        _store.Set(null);
        PublishState();

        _console.WriteLine("Status: off");
    }

    private void StartSignal(UserMood mood, TaskCompletionSource<IntentOutcome> completion, CancellationToken cancellationToken) {
        string name = UserMoodConverter.ToCanonicalValue(mood);

        // Checked in the priority order of spec §23: off wins over everything, then the lock.
        if (_desired is null) {
            _console.WriteLine($"SignalMe is off: '{name}' is not played.");
            completion.TrySetResult(IntentOutcome.SignalRefusedOff);

            return;
        }
        if (_session == SessionState.Locked) {
            _console.WriteLine($"Session locked: '{name}' is not played.");
            completion.TrySetResult(IntentOutcome.SignalRefusedSessionLocked);

            return;
        }

        _console.WriteLine($"Playing: {name}");

        // Published before the first frame goes out, so a mode reading the state while the LEDs animate
        // is never told that nothing is playing.
        PublishState(mood);

        // The signal's own source, linked to the loop's: interrupting one signal must not stop the loop,
        // while stopping the loop must stop the signal.
        CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<UserStatus?>       animation;
        try {
            animation = MoodPatternFactory.Create(mood, _device, _delay).PlayAsync(_desired, cancellation.Token);
        } catch {
            cancellation.Dispose();

            throw;
        }

        long id = ++_lastSignalId;
        _running = new RunningSignal(id, mood, animation, cancellation, completion);

        // The end of the animation is posted as a message rather than awaited here, so that the loop keeps
        // reading: a lock or a new intent must be able to interrupt it. Synchronous, so the message is in
        // the channel the moment the task completes.
        _ = animation.ContinueWith(task => _channel.Writer.TryWrite(new Message.SignalEnded(id, task)), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    private void HandleSessionChanged(SessionState session) {
        if (_desired is not null) { Render(_desired, session); }
        _session = session;
        PublishState();

        // One block, so that a console repeating a pending prompt does so once, after both lines.
        string header = session == SessionState.Locked ? "Windows session locked." : "Windows session unlocked.";
        _console.WriteLine($"{header}{Environment.NewLine}Effective status: {EffectiveStatusConverter.ToCanonicalValue(_state.Effective)}");
    }

    /// <summary>
    ///     Stops the running signal and settles it. A signal whose task had already completed is handled as
    ///     completed, whatever brought the loop to look at it: a sender is never told its signal was
    ///     interrupted when it ran to its end.
    /// </summary>
    private async Task InterruptRunningSignalAsync() {
        _running!.Cancellation.Cancel();
        await AwaitQuietlyAsync(_running.Animation).ConfigureAwait(false);
        await SettleRunningSignalAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Takes the completed animation out of the loop's hands and completes its sender: as completed
    ///     when it ran to its end, as a fault when it failed, as interrupted when it was cancelled. No
    ///     restore write happens for an interrupted signal: whatever comes next decides what is displayed.
    /// </summary>
    private async Task SettleRunningSignalAsync() {
        RunningSignal running = _running!;
        _running = null;
        try {
            if (running.Animation.IsCompletedSuccessfully) {
                CompleteSignal(running, await running.Animation.ConfigureAwait(false));
            } else if (FailureOf(running.Animation) is { } failure) {
                running.Completion.TrySetException(failure);
                ExceptionDispatchInfo.Capture(failure).Throw();
            } else {
                running.Completion.TrySetResult(IntentOutcome.SignalInterrupted);
            }
        } finally {
            running.Dispose();
            PublishState();
        }
    }

    /// <summary>
    ///     Displays what a finished signal leaves behind: a new status when it changed it ("ready" ends on
    ///     available, which is then persisted), the status it ran over otherwise. Never off: a signal only
    ///     ever runs over a durable status.
    /// </summary>
    private void CompleteSignal(RunningSignal running, UserStatus? result) {
        try {
            if (result != _desired) {
                Render(result, _session);
                _desired = result;
                _store.Set(result);
                PublishState();
                _console.WriteLine($"Status: {Describe(result)}");
            } else {
                Render(_desired, _session);
                PublishState();
                _console.WriteLine($"Restored: {Describe(_desired)}");
            }

            running.Completion.TrySetResult(IntentOutcome.SignalCompleted);
        } catch (Exception exception) {
            running.Completion.TrySetException(exception);

            throw;
        }
    }

    /// <summary>
    ///     One device write for the effective status of <paramref name="desired" /> in <paramref name="session" />.
    /// </summary>
    private void Render(UserStatus? desired, SessionState session) {
        EffectiveStatus effective = ComputeEffective(desired, session);
        if (effective == EffectiveStatus.Off) {
            _device.TurnOffOrThrow();
        } else {
            _device.SetColorOrThrow(StatusColors.For(effective), $"display the '{EffectiveStatusConverter.ToCanonicalValue(effective)}' status");
        }
    }

    /// <summary>
    ///     Leaves nothing behind when the loop exits, on cancellation and on fault alike: the running
    ///     signal is stopped and its sender told, then every intent still queued, or posted from now on, is
    ///     completed. Nothing is written to the device here, the runtime turns it off next.
    /// </summary>
    private async Task ShutdownAsync(Exception? fault, CancellationToken cancellationToken) {
        if (_running is not null) {
            RunningSignal running = _running;
            _running = null;
            running.Cancellation.Cancel();
            await AwaitQuietlyAsync(running.Animation).ConfigureAwait(false);
            // A signal that had finished is still reported as completed, even though its result is not
            // applied: the device is about to be turned off anyway.
            running.Completion.TrySetResult(running.Animation.IsCompletedSuccessfully ? IntentOutcome.SignalCompleted : IntentOutcome.SignalInterrupted);
            running.Dispose();
        }

        _channel.Writer.TryComplete();
        while (_channel.Reader.TryRead(out Message? message)) {
            if (message is not Message.Intent intent) { continue; }

            if (fault is not null) {
                intent.Completion.TrySetException(fault);
            } else {
                intent.Completion.TrySetCanceled(cancellationToken);
            }
        }

        PublishState();
    }

    private void PublishState() {
        PublishState(_running?.Mood);
    }

    private void PublishState(UserMood? playingSignal) {
        Volatile.Write(ref _state, new SignalMeState(_desired, _session, ComputeEffective(_desired, _session), playingSignal));
    }

    #region Nested types declarations

    private abstract record Message {

        #region Nested types declarations

        public sealed record Intent(SignalMeIntent Value, TaskCompletionSource<IntentOutcome> Completion) : Message;

        public sealed record SessionChanged(SessionState State) : Message;

        public sealed record SignalEnded(long SignalId, Task<UserStatus?> Animation) : Message;

        #endregion

    }

    private sealed class RunningSignal : IDisposable {

        #region Constructors declarations

        public RunningSignal(long id, UserMood mood, Task<UserStatus?> animation, CancellationTokenSource cancellation, TaskCompletionSource<IntentOutcome> completion) {
            Id           = id;
            Mood         = mood;
            Animation    = animation;
            Cancellation = cancellation;
            Completion   = completion;
        }

        #endregion

        public long                                Id           { get; }
        public UserMood                            Mood         { get; }
        public Task<UserStatus?>                   Animation    { get; }
        public CancellationTokenSource             Cancellation { get; }
        public TaskCompletionSource<IntentOutcome> Completion   { get; }

        public void Dispose() {
            Cancellation.Dispose();
        }

    }

    #endregion

}
