using SignalMe.Modes;
using SignalMe.Runtime;
using SignalMe.Services;
using SignalMe.Sessions;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     The lifecycle: what is rendered before the mode starts, how a run stops, and what is left of the
///     device afterwards, whatever happened.
/// </summary>
public sealed class SignalMeRuntimeTests {

    /// <summary>Derived from the enum, so adding a mood cannot silently leave it untested.</summary>
    public static TheoryData<UserMood> AllMoods => new(Enum.GetValues<UserMood>());

    public static TheoryData<UserStatus?, SessionState, string> InitialRenders => new() {
        { UserStatus.Busy, SessionState.Active, "SetColor(#FFFF00)" },
        { UserStatus.Busy, SessionState.Locked, "SetColor(#9932CC)" },
        { null, SessionState.Active, "TurnOff" }
    };

    #region start-up

    [Theory]
    [MemberData(nameof(InitialRenders))]
    public async Task The_initial_render_comes_from_the_store_and_the_session(UserStatus? stored, SessionState session, string expected) {
        using Fixture signalme = new(stored, session);

        RuntimeOutcome outcome = await signalme.RunAsync(FakeMode.Returning());

        Check.That(outcome).IsEqualTo(RuntimeOutcome.Stopped);
        Check.That(signalme.Device.Commands[0]).IsEqualTo(expected);
    }

    [Fact]
    public async Task The_status_is_rendered_and_printed_before_the_mode_starts() {
        using Fixture signalme = new(UserStatus.Busy);
        string[]      outputAtStart   = [];
        string[]      commandsAtStart = [];
        FakeMode mode = new((_, cancellationToken) => {
            outputAtStart   = signalme.Console.Output.ToArray();
            commandsAtStart = signalme.Device.Commands.ToArray();
            signalme.Console.WriteLine("Commands: help");
            signalme.Console.Write("> ");

            return signalme.Console.ReadLineAsync(cancellationToken);
        });

        await signalme.RunAsync(mode);

        Check.That(outputAtStart).ContainsExactly(["Status: busy"]);
        Check.That(commandsAtStart).ContainsExactly(["SetColor(#FFFF00)"]);
        Check.That(signalme.Console.Transcript).ContainsExactly(["line: Status: busy", "line: Commands: help", "prompt: > ", "read: <end of input>", "line: Stopping SignalMe...", "line: SignalMe stopped."]);
    }

    [Fact]
    public async Task A_refused_initial_render_stops_before_the_mode_starts() {
        using Fixture signalme = new(UserStatus.Busy);
        signalme.Device.RefuseFromCall = 1;
        FakeMode mode = FakeMode.Returning();

        RuntimeOutcome outcome = await signalme.RunAsync(mode);

        Check.That(outcome).IsEqualTo(RuntimeOutcome.DeviceFailed);
        Check.That(mode.Started).IsFalse();
        Check.That(signalme.Console.Error).Contains("The Luxafor device refused to display the 'busy' status.");
        Check.That(signalme.Console.Output).ContainsExactly(["SignalMe stopped."]);
        Check.That(signalme.Device.IsDisposed).IsTrue();
        Check.That(signalme.Monitor.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_monitor_whose_start_finds_the_session_locked_renders_away_first() {
        using Fixture signalme = new(UserStatus.Busy);
        signalme.Monitor.StateOnStart = SessionState.Locked;

        await signalme.RunAsync(FakeMode.Returning());

        Check.That(signalme.Device.Commands[0]).IsEqualTo("SetColor(#9932CC)");
        Check.That(signalme.Console.Output).ContainsExactly(["Status: busy", "Effective status: away", "Stopping SignalMe...", "SignalMe stopped."]);
    }

    /// <summary>
    ///     The handler is subscribed before Start(), so a change raised from inside it is queued; the first
    ///     render already reflects it, and the queued duplicate is dropped rather than rendered twice.
    /// </summary>
    [Fact]
    public async Task A_lock_raised_from_inside_start_is_rendered_once() {
        using Fixture signalme = new(UserStatus.Busy);
        signalme.Monitor.StateOnStart = SessionState.Locked;
        signalme.Monitor.RaiseOnStart = true;
        // A refused signal writes nothing to the device, and is answered only after the queued lock was
        // looked at, which is what this test needs to observe.
        FakeMode mode = FakeMode.Sending(new SignalMeIntent.PlaySignal(UserMood.Happy));

        await signalme.RunAsync(mode);

        Check.That(mode.Outcomes).ContainsExactly([IntentOutcome.SignalRefusedSessionLocked]);
        Check.That(signalme.Device.Commands).ContainsExactly(["SetColor(#9932CC)", "TurnOff"]);
        Check.That(signalme.Console.Output).ContainsExactly(["Status: busy", "Effective status: away", "Session locked: 'happy' is not played.", "Stopping SignalMe...", "SignalMe stopped."]);
    }

    [Fact]
    public async Task A_monitor_that_cannot_start_stops_the_run_with_the_device_off() {
        using Fixture signalme = new(UserStatus.Busy);
        signalme.Monitor.StartFailure = new InvalidOperationException("no window");
        FakeMode mode = FakeMode.Returning();

        RuntimeOutcome outcome = await signalme.RunAsync(mode);

        Check.That(outcome).IsEqualTo(RuntimeOutcome.Faulted);
        Check.That(mode.Started).IsFalse();
        Check.That(signalme.Console.Error).ContainsExactly(["signalme: InvalidOperationException: no window"]);
        Check.That(signalme.Device.Commands).ContainsExactly(["TurnOff"]);
        Check.That(signalme.Device.IsDisposed).IsTrue();
        Check.That(signalme.Monitor.IsDisposed).IsTrue();
        Check.That(signalme.DeviceMonitor.IsDisposed).IsTrue();
    }

    #endregion

    #region stopping

    [Fact]
    public async Task Cancelling_the_token_stops_everything_and_turns_the_device_off() {
        using Fixture signalme = new(UserStatus.Busy);
        FakeMode mode = new((_, cancellationToken) => {
            signalme.Cancellation.Cancel();

            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });

        RuntimeOutcome outcome = await signalme.RunAsync(mode);

        Check.That(outcome).IsEqualTo(RuntimeOutcome.Stopped);
        Check.That(signalme.Console.Output).ContainsExactly(["Status: busy", "Stopping SignalMe...", "SignalMe stopped."]);
        Check.That(signalme.Device.Commands).ContainsExactly(["SetColor(#FFFF00)", "TurnOff"]);
        Check.That(signalme.Device.IsDisposed).IsTrue();
        Check.That(signalme.Monitor.Started).IsTrue();
        Check.That(signalme.Monitor.IsDisposed).IsTrue();
        Check.That(signalme.DeviceMonitor.Started).IsTrue();
        Check.That(signalme.DeviceMonitor.IsDisposed).IsTrue();
        Check.That(signalme.DeviceMonitor.IsSubscribed).IsFalse();
        Check.That(signalme.Console.Error).IsEmpty();
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public async Task Cancelling_during_an_animation_ends_with_a_single_turn_off(UserMood mood) {
        using Fixture signalme = new(UserStatus.Busy);
        signalme.Delay.OnWait = wait => { if (wait == 3) { signalme.Cancellation.Cancel(); } };

        RuntimeOutcome outcome = await signalme.RunAsync(FakeMode.Sending(new SignalMeIntent.PlaySignal(mood)));

        Check.That(outcome).IsEqualTo(RuntimeOutcome.Stopped);
        Check.That(signalme.Device.Commands[^1]).IsEqualTo("TurnOff");
        // No restore frame and no second turn-off: the animation stopped, then the device was turned off, once.
        Check.That(signalme.Device.Commands[^2]).IsNotEqualTo("TurnOff");
        Check.That(signalme.Delay.Waits).IsEqualTo(3);
        Check.That(signalme.Device.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_mode_that_returns_stops_the_run() {
        using Fixture signalme = new(UserStatus.Busy);
        FakeMode      mode     = FakeMode.Sending(new SignalMeIntent.SetDesiredStatus(UserStatus.Available), new SignalMeIntent.PlaySignal(UserMood.Happy));

        RuntimeOutcome outcome = await signalme.RunAsync(mode);

        Check.That(outcome).IsEqualTo(RuntimeOutcome.Stopped);
        Check.That(mode.Outcomes).ContainsExactly([IntentOutcome.Applied, IntentOutcome.SignalCompleted]);
        Check.That(signalme.Device.Commands[^1]).IsEqualTo("TurnOff");
        Check.That(signalme.Statuses.Store.Get()).IsEqualTo(UserStatus.Available);
        Check.That(signalme.Console.Output[^1]).IsEqualTo("SignalMe stopped.");
    }

    [Fact]
    public async Task A_session_change_reaches_the_coordinator_through_the_monitor() {
        using Fixture signalme = new(UserStatus.Busy);
        FakeMode mode = new(async (context, _) => {
            signalme.Monitor.Raise(SessionState.Locked);
            await signalme.Console.WaitForOutputAsync(2);
            Check.That(context.State.Effective).IsEqualTo(EffectiveStatus.Away);
            signalme.Monitor.Raise(SessionState.Active);
            await signalme.Console.WaitForOutputAsync(3);
        });

        await signalme.RunAsync(mode);

        Check.That(signalme.Device.Commands).ContainsExactly(["SetColor(#FFFF00)", "SetColor(#9932CC)", "SetColor(#FFFF00)", "TurnOff"]);
    }

    #endregion

    #region failures

    [Fact]
    public async Task A_device_refusal_during_the_run_ends_it_as_a_device_failure() {
        using Fixture signalme = new();
        signalme.Device.RefuseFromCall = 2;
        FakeMode mode = FakeMode.Sending(new SignalMeIntent.SetDesiredStatus(UserStatus.Busy));

        RuntimeOutcome outcome = await signalme.RunAsync(mode);

        Check.That(outcome).IsEqualTo(RuntimeOutcome.DeviceFailed);
        // Reported once by the loop; the mode rethrows the same failure, which must not print it again.
        Check.That(signalme.Console.Error.Where(line => line == "The Luxafor device refused to display the 'busy' status.")).HasSize(1);
        Check.That(signalme.Console.Output[^1]).IsEqualTo("SignalMe stopped.");
        Check.That(signalme.Device.IsDisposed).IsTrue();
        Check.That(signalme.Monitor.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_device_found_unplugged_ends_the_run_as_a_device_failure_without_writing_to_it() {
        using Fixture signalme = new(UserStatus.Busy);
        FakeMode mode = new((_, cancellationToken) => {
            signalme.DeviceMonitor.Disconnect();

            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        });

        RuntimeOutcome outcome = await signalme.RunAsync(mode);

        Check.That(outcome).IsEqualTo(RuntimeOutcome.DeviceFailed);
        Check.That(signalme.Console.Transcript).ContainsExactly(["line: Status: busy", "error: Luxafor device disconnected.", "line: Stopping SignalMe...", "line: SignalMe stopped."]);
        // No final turn-off: it could only fail on a device that is gone, and add a second error.
        Check.That(signalme.Device.Commands).ContainsExactly(["SetColor(#FFFF00)"]);
        Check.That(signalme.Device.IsDisposed).IsTrue();
        Check.That(signalme.DeviceMonitor.IsDisposed).IsTrue();
        Check.That(signalme.DeviceMonitor.IsSubscribed).IsFalse();
    }

    [Fact]
    public async Task A_mode_that_fails_ends_the_run_as_faulted_and_reports_it() {
        using Fixture signalme = new(UserStatus.Busy);

        RuntimeOutcome outcome = await signalme.RunAsync(FakeMode.Throwing(new InvalidOperationException("boom")));

        Check.That(outcome).IsEqualTo(RuntimeOutcome.Faulted);
        Check.That(signalme.Console.Error).ContainsExactly(["signalme: InvalidOperationException: boom"]);
        Check.That(signalme.Device.Commands).ContainsExactly(["SetColor(#FFFF00)", "TurnOff"]);
        Check.That(signalme.Device.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_mode_that_throws_before_its_first_await_is_handled_like_any_other_failure() {
        using Fixture signalme = new(UserStatus.Busy);

        RuntimeOutcome outcome = await signalme.RunAsync(new FakeMode((_, _) => throw new InvalidOperationException("early")));

        Check.That(outcome).IsEqualTo(RuntimeOutcome.Faulted);
        Check.That(signalme.Console.Error).ContainsExactly(["signalme: InvalidOperationException: early"]);
        Check.That(signalme.Device.Commands[^1]).IsEqualTo("TurnOff");
        Check.That(signalme.Device.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_refused_final_turn_off_is_reported_but_does_not_change_the_outcome() {
        using Fixture signalme = new(UserStatus.Busy);
        FakeMode mode = new((_, _) => {
            signalme.Device.RefuseFromCall = 2;

            return Task.CompletedTask;
        });

        RuntimeOutcome outcome = await signalme.RunAsync(mode);

        Check.That(outcome).IsEqualTo(RuntimeOutcome.Stopped);
        Check.That(signalme.Console.Error).ContainsExactly(["The Luxafor device refused to turn its LEDs off."]);
        Check.That(signalme.Device.IsDisposed).IsTrue();
    }

    #endregion

    /// <summary>
    ///     A runtime over fakes; the mode is the one thing each test chooses.
    /// </summary>
    private sealed class Fixture : IDisposable {

        public Fixture(UserStatus? stored = null, SessionState session = SessionState.Active) {
            if (stored is not null) { Statuses.Store.Set(stored); }

            Monitor = new FakeSessionMonitor(session);
        }

        public FakeLuxaforDevice           Device        { get; } = new();
        public TemporaryStatusStore        Statuses      { get; } = new();
        public FakeConsole                 Console       { get; } = new();
        public InstantDelay                Delay         { get; } = new();
        public FakeSessionMonitor          Monitor       { get; }
        public FakeDeviceConnectionMonitor DeviceMonitor { get; } = new();
        public CancellationTokenSource     Cancellation  { get; } = new();

        public Task<RuntimeOutcome> RunAsync(ISignalMeMode mode) {
            return new SignalMeRuntime("manual", mode, Device, DeviceMonitor, Monitor, Statuses.Store, Console, Delay).RunAsync(Cancellation.Token);
        }

        public void Dispose() {
            Cancellation.Dispose();
            Statuses.Dispose();
        }

    }

}
