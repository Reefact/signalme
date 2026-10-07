using SignalMe.Infrastructure;
using SignalMe.Runtime;
using SignalMe.Services;
using SignalMe.Sessions;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     The one owner of the device: what it shows for each combination of desired status and session, how
///     signals are played, interrupted or refused, and what it leaves behind when it stops.
/// </summary>
public sealed class StatusCoordinatorTests {

    private static readonly string NewLine = Environment.NewLine;

    private const string Available = "SetColor(#00FF00)";
    private const string Busy      = "SetColor(#FFFF00)";
    private const string Dnd       = "SetColor(#FF0000)";
    private const string Away      = "SetColor(#9932CC)";
    private const string Off       = "TurnOff";

    public static TheoryData<UserStatus, SessionState, string> EffectiveMatrix => new() {
        { UserStatus.Available, SessionState.Active, Available },
        { UserStatus.Busy, SessionState.Active, Busy },
        { UserStatus.DoNotDisturb, SessionState.Active, Dnd },
        { UserStatus.Available, SessionState.Locked, Away },
        { UserStatus.Busy, SessionState.Locked, Away },
        { UserStatus.DoNotDisturb, SessionState.Locked, Away }
    };

    #region effective status

    [Theory]
    [MemberData(nameof(EffectiveMatrix))]
    public async Task The_initial_render_shows_the_desired_status_unless_the_session_is_locked(UserStatus desired, SessionState session, string expected) {
        await using Harness signalme = new(desired, session);

        Assert.Equal([expected], signalme.Device.Commands);
        Assert.Equal(desired, signalme.State.DesiredStatus);
        Assert.Equal(session, signalme.State.Session);
        Assert.Equal(session == SessionState.Locked ? EffectiveStatus.Away : Enum.Parse<EffectiveStatus>(desired.ToString()), signalme.State.Effective);
    }

    [Fact]
    public async Task The_initial_render_says_what_it_shows() {
        await using Harness signalme = new(UserStatus.Busy, SessionState.Locked);

        Assert.Equal(["Status: busy", "Effective status: away"], signalme.Console.Output);
    }

    [Fact]
    public async Task Busy_then_lock_shows_away_then_unlock_shows_busy_again() {
        await using Harness signalme = new(UserStatus.Busy);

        await signalme.LockAsync();
        await signalme.UnlockAsync();

        Assert.Equal([Busy, Away, Busy], signalme.Device.Commands);
        Assert.Equal(["Status: busy", "Windows session locked." + NewLine + "Effective status: away", "Windows session unlocked." + NewLine + "Effective status: busy"], signalme.Console.Output);
        Assert.Equal(UserStatus.Busy, signalme.State.DesiredStatus);
        Assert.Equal(EffectiveStatus.Busy, signalme.State.Effective);
    }

    [Fact]
    public async Task Off_stays_off_through_a_lock_and_an_unlock() {
        await using Harness signalme = new();

        await signalme.LockAsync();
        await signalme.UnlockAsync();

        // Nothing to render: the device is off and stays off, the session is only reported.
        Assert.Equal([Off], signalme.Device.Commands);
        Assert.Equal(["Status: off", "Windows session locked." + NewLine + "Effective status: off", "Windows session unlocked." + NewLine + "Effective status: off"], signalme.Console.Output);
        Assert.Equal(EffectiveStatus.Off, signalme.State.Effective);
    }

    [Fact]
    public async Task A_status_set_while_locked_is_rendered_as_away_and_shown_once_unlocked() {
        await using Harness signalme = new(UserStatus.Busy);
        await signalme.LockAsync();

        Assert.Equal(IntentOutcome.Applied, await signalme.SetAsync(UserStatus.Available));
        await signalme.UnlockAsync();

        Assert.Equal([Busy, Away, Away, Available], signalme.Device.Commands);
        Assert.Contains("Status: available", signalme.Console.Output);
        Assert.Contains("Effective status: away", signalme.Console.Output);
        Assert.Equal(UserStatus.Available, signalme.Statuses.Store.Get());
    }

    [Fact]
    public async Task A_duplicate_session_state_is_ignored() {
        await using Harness signalme = new(UserStatus.Busy);

        signalme.Coordinator.OnSessionChanged(SessionState.Active);
        // Ordered behind the duplicate: once this is answered, the duplicate has been looked at.
        await signalme.SetAsync(UserStatus.Busy);

        Assert.Equal([Busy, Busy], signalme.Device.Commands);
        Assert.DoesNotContain(signalme.Console.Output, line => line.Contains("Windows session"));
    }

    [Fact]
    public async Task Away_is_not_a_status_a_mode_may_ask_for() {
        await using Harness signalme = new(UserStatus.Busy);

        await Assert.ThrowsAsync<ArgumentException>(() => signalme.SetAsync(UserStatus.Away));

        Assert.Equal([Busy], signalme.Device.Commands);
    }

    #endregion

    #region persistence

    [Fact]
    public async Task A_status_is_remembered_once_displayed() {
        await using Harness signalme = new();

        Assert.Equal(IntentOutcome.Applied, await signalme.SetAsync(UserStatus.DoNotDisturb));

        Assert.Equal([Off, Dnd], signalme.Device.Commands);
        Assert.Equal(["Status: off", "Status: do-not-disturb"], signalme.Console.Output);
        Assert.Equal(UserStatus.DoNotDisturb, signalme.Statuses.Store.Get());
    }

    [Fact]
    public async Task A_lock_does_not_change_the_remembered_status() {
        await using Harness signalme = new(UserStatus.Busy);

        await signalme.LockAsync();
        Assert.Equal(UserStatus.Busy, signalme.Statuses.Store.Get());

        await signalme.UnlockAsync();
        Assert.Equal(UserStatus.Busy, signalme.Statuses.Store.Get());
    }

    [Fact]
    public async Task A_refused_status_is_not_remembered_and_stops_the_loop() {
        await using Harness signalme = new(UserStatus.Busy);
        signalme.Device.RefuseFromCall = 2;

        await Assert.ThrowsAsync<DeviceCommandFailedException>(() => signalme.SetAsync(UserStatus.Available));

        Assert.IsType<DeviceCommandFailedException>(await signalme.StopAsync());
        // The remembered status must keep describing what the LEDs are actually showing.
        Assert.Equal(UserStatus.Busy, signalme.Statuses.Store.Get());
        Assert.Equal(UserStatus.Busy, signalme.State.DesiredStatus);
    }

    [Fact]
    public async Task Turning_off_forgets_the_status_only_once_the_device_obeyed() {
        await using Harness refusing = new(UserStatus.Busy);
        refusing.Device.RefuseFromCall = 2;

        await Assert.ThrowsAsync<DeviceCommandFailedException>(() => refusing.ExecuteAsync(new SignalMeIntent.TurnOff()));
        Assert.Equal(UserStatus.Busy, refusing.Statuses.Store.Get());

        await using Harness obeying = new(UserStatus.Busy);

        Assert.Equal(IntentOutcome.Applied, await obeying.ExecuteAsync(new SignalMeIntent.TurnOff()));
        Assert.Null(obeying.Statuses.Store.Get());
        Assert.Equal([Busy, Off], obeying.Device.Commands);
        Assert.Equal(["Status: busy", "Status: off"], obeying.Console.Output);
        Assert.Null(obeying.State.DesiredStatus);
        Assert.Equal(EffectiveStatus.Off, obeying.State.Effective);
    }

    #endregion

    #region signals

    [Fact]
    public async Task A_signal_plays_and_the_status_comes_back() {
        await using Harness signalme = new(UserStatus.Busy);

        Assert.Equal(IntentOutcome.SignalCompleted, await signalme.PlayAsync(UserMood.Happy));

        Assert.Equal(Busy, signalme.Device.LastCommand);
        Assert.True(signalme.Device.Commands.Count > 2, "expected the animation to have sent frames.");
        Assert.Equal(["Status: busy", "Playing: happy", "Restored: busy"], signalme.Console.Output);
        Assert.Equal(UserStatus.Busy, signalme.Statuses.Store.Get());
        Assert.Null(signalme.State.PlayingSignal);
    }

    [Fact]
    public async Task Ready_ends_on_available_and_remembers_it() {
        await using Harness signalme = new(UserStatus.Busy);

        Assert.Equal(IntentOutcome.SignalCompleted, await signalme.PlayAsync(UserMood.Ready));

        Assert.Equal(Available, signalme.Device.LastCommand);
        Assert.Equal(["Status: busy", "Playing: ready", "Status: available"], signalme.Console.Output);
        Assert.Equal(UserStatus.Available, signalme.Statuses.Store.Get());
        Assert.Equal(UserStatus.Available, signalme.State.DesiredStatus);
    }

    [Fact]
    public async Task The_state_reports_the_signal_while_it_plays() {
        await using Harness signalme = new(UserStatus.Busy);
        ControllableDelay.Hold hold = signalme.Delay.HoldAt(3);

        Task<IntentOutcome> signal = signalme.PlayAsync(UserMood.Happy);
        await hold.Reached;

        Assert.Equal(UserMood.Happy, signalme.State.PlayingSignal);
        Assert.Equal(UserStatus.Busy, signalme.State.DesiredStatus);

        hold.Release();
        Assert.Equal(IntentOutcome.SignalCompleted, await signal);
        Assert.Null(signalme.State.PlayingSignal);
    }

    [Fact]
    public async Task A_lock_interrupts_a_signal_and_shows_away_at_once() {
        await using Harness signalme = new(UserStatus.Busy);
        ControllableDelay.Hold hold = signalme.Delay.HoldAt(3);

        Task<IntentOutcome> signal = signalme.PlayAsync(UserMood.Happy);
        await hold.Reached;
        int framesSent = signalme.Device.Commands.Count;

        await signalme.LockAsync();

        Assert.Equal(IntentOutcome.SignalInterrupted, await signal);
        // Away right after the last frame: no restore frame, nothing of the animation after it.
        Assert.Equal(framesSent + 1, signalme.Device.Commands.Count);
        Assert.Equal(Away, signalme.Device.LastCommand);
        Assert.DoesNotContain(signalme.Console.Output, line => line.StartsWith("Restored", StringComparison.Ordinal));
        Assert.Null(signalme.State.PlayingSignal);
        Assert.Equal(UserStatus.Busy, signalme.Statuses.Store.Get());
    }

    [Fact]
    public async Task An_interrupted_signal_does_not_resume_once_unlocked() {
        await using Harness signalme = new(UserStatus.Busy);
        ControllableDelay.Hold hold = signalme.Delay.HoldAt(3);

        Task<IntentOutcome> signal = signalme.PlayAsync(UserMood.Happy);
        await hold.Reached;
        await signalme.LockAsync();
        await signal;

        await signalme.UnlockAsync();

        Assert.Equal(Busy, signalme.Device.LastCommand);
        Assert.Equal(3, signalme.Delay.Waits);
        Assert.Equal(["Status: busy", "Playing: happy", "Windows session locked." + NewLine + "Effective status: away", "Windows session unlocked." + NewLine + "Effective status: busy"], signalme.Console.Output);
    }

    [Fact]
    public async Task A_status_set_during_a_signal_interrupts_it_and_applies_at_once() {
        await using Harness signalme = new(UserStatus.Busy);
        ControllableDelay.Hold hold = signalme.Delay.HoldAt(3);

        Task<IntentOutcome> signal = signalme.PlayAsync(UserMood.Desperate);
        await hold.Reached;
        int framesSent = signalme.Device.Commands.Count;

        Assert.Equal(IntentOutcome.Applied, await signalme.SetAsync(UserStatus.Available));

        Assert.Equal(IntentOutcome.SignalInterrupted, await signal);
        Assert.Equal(framesSent + 1, signalme.Device.Commands.Count);
        Assert.Equal(Available, signalme.Device.LastCommand);
        Assert.Equal(UserStatus.Available, signalme.Statuses.Store.Get());
        Assert.Equal(["Status: busy", "Playing: desperate", "Status: available"], signalme.Console.Output);
    }

    [Fact]
    public async Task A_signal_replaces_the_one_playing() {
        await using Harness signalme = new(UserStatus.Busy);
        ControllableDelay.Hold hold = signalme.Delay.HoldAt(3);

        Task<IntentOutcome> first = signalme.PlayAsync(UserMood.Happy);
        await hold.Reached;
        Task<IntentOutcome> second = signalme.PlayAsync(UserMood.Alerting);

        Assert.Equal(IntentOutcome.SignalInterrupted, await first);
        Assert.Equal(IntentOutcome.SignalCompleted, await second);
        Assert.Equal(["Status: busy", "Playing: happy", "Playing: alerting", "Restored: busy"], signalme.Console.Output);
        Assert.Equal(Busy, signalme.Device.LastCommand);
    }

    [Fact]
    public async Task Turning_off_during_a_signal_interrupts_it() {
        await using Harness signalme = new(UserStatus.Busy);
        ControllableDelay.Hold hold = signalme.Delay.HoldAt(3);

        Task<IntentOutcome> signal = signalme.PlayAsync(UserMood.Happy);
        await hold.Reached;

        Assert.Equal(IntentOutcome.Applied, await signalme.ExecuteAsync(new SignalMeIntent.TurnOff()));

        Assert.Equal(IntentOutcome.SignalInterrupted, await signal);
        Assert.Equal(Off, signalme.Device.LastCommand);
        Assert.Null(signalme.Statuses.Store.Get());
    }

    [Fact]
    public async Task A_signal_asked_while_locked_is_refused_without_touching_the_device() {
        await using Harness signalme = new(UserStatus.Busy);
        await signalme.LockAsync();

        Assert.Equal(IntentOutcome.SignalRefusedSessionLocked, await signalme.PlayAsync(UserMood.Happy));

        Assert.Equal([Busy, Away], signalme.Device.Commands);
        Assert.Contains("Session locked: 'happy' is not played.", signalme.Console.Output);
        Assert.Equal(UserStatus.Busy, signalme.Statuses.Store.Get());
    }

    [Theory]
    [InlineData(UserMood.Happy)]
    [InlineData(UserMood.Ready)]
    public async Task A_signal_asked_while_off_is_refused_without_touching_anything(UserMood mood) {
        await using Harness signalme = new();
        SignalMeState     before   = signalme.State;

        Assert.Equal(IntentOutcome.SignalRefusedOff, await signalme.PlayAsync(mood));

        Assert.Equal([Off], signalme.Device.Commands);
        Assert.Contains($"SignalMe is off: '{mood.ToString().ToLowerInvariant()}' is not played.", signalme.Console.Output);
        Assert.Null(signalme.Statuses.Store.Get());
        Assert.Equal(before, signalme.State);
    }

    [Fact]
    public async Task Off_wins_over_the_lock_when_refusing_a_signal() {
        await using Harness signalme = new(null, SessionState.Locked);

        Assert.Equal(IntentOutcome.SignalRefusedOff, await signalme.PlayAsync(UserMood.Happy));

        Assert.Contains("SignalMe is off: 'happy' is not played.", signalme.Console.Output);
        Assert.DoesNotContain(signalme.Console.Output, line => line.StartsWith("Session locked", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Pinned as far as the seams allow: a duplicate that wrongly interrupted the signal at any time
    ///     before the fourth wait is released would keep the animation from reaching that wait, or from
    ///     leaving it uncancelled, and the test fails. Nothing observable marks a dropped duplicate, so a
    ///     loop that only read it after that release, once the remaining instant waits had run, could in
    ///     theory still hide the regression; no seam closes that last window.
    /// </summary>
    [Fact]
    public async Task A_duplicate_session_state_does_not_interrupt_a_signal() {
        await using Harness signalme = new(UserStatus.Busy);
        ControllableDelay.Hold third  = signalme.Delay.HoldAt(3);
        ControllableDelay.Hold fourth = signalme.Delay.HoldAt(4);

        Task<IntentOutcome> signal = signalme.PlayAsync(UserMood.Happy);
        await third.Reached;
        signalme.Coordinator.OnSessionChanged(SessionState.Active);
        third.Release();

        // Reached only by an animation nobody cancelled before its fourth wait.
        await fourth.Reached;
        Assert.Equal(UserMood.Happy, signalme.State.PlayingSignal);
        Assert.False(signal.IsCompleted);
        fourth.Release();

        Assert.Equal(IntentOutcome.SignalCompleted, await signal);
        Assert.True(signalme.Delay.Waits > 4, "expected the animation to have run past the held waits.");
        Assert.Equal(["Status: busy", "Playing: happy", "Restored: busy"], signalme.Console.Output);
    }

    /// <summary>
    ///     The lock is queued while the animation runs and read once it has finished: the signal ran to its
    ///     end, so it is handled as completed, its result applied, and only then does the lock apply.
    /// </summary>
    [Fact]
    public async Task A_signal_that_completed_just_as_a_lock_arrives_is_handled_as_completed() {
        await using Harness signalme = new(UserStatus.Busy);
        signalme.Delay.OnWait = wait => { if (wait == 3) { signalme.Coordinator.OnSessionChanged(SessionState.Locked); } };

        Assert.Equal(IntentOutcome.SignalCompleted, await signalme.PlayAsync(UserMood.Ready));
        await signalme.Console.WaitForOutputAsync(4);

        Assert.Equal(UserStatus.Available, signalme.Statuses.Store.Get());
        Assert.Equal(["Status: busy", "Playing: ready", "Status: available", "Windows session locked." + NewLine + "Effective status: away"], signalme.Console.Output);
        Assert.Equal([Available, Away], signalme.Device.Commands.TakeLast(2));
    }

    #endregion

    #region shutdown and faults

    [Fact]
    public async Task Stopping_the_loop_during_a_signal_stops_the_animation_without_a_restore_frame() {
        await using Harness signalme = new(UserStatus.Busy);
        ControllableDelay.Hold hold = signalme.Delay.HoldAt(3);

        Task<IntentOutcome> signal = signalme.PlayAsync(UserMood.Happy);
        await hold.Reached;
        int framesSent = signalme.Device.Commands.Count;

        Assert.Null(await signalme.StopAsync());

        Assert.Equal(IntentOutcome.SignalInterrupted, await signal);
        // The runtime turns the device off next; the loop itself writes nothing on its way out.
        Assert.Equal(framesSent, signalme.Device.Commands.Count);
        Assert.Equal(3, signalme.Delay.Waits);
        Assert.Null(signalme.State.PlayingSignal);
    }

    [Fact]
    public async Task An_intent_pending_when_the_loop_is_stopped_completes_cancelled() {
        await using Harness signalme = new(UserStatus.Busy);
        Task<IntentOutcome>? queued = null;
        // Queued from inside the animation, so it is in the channel when the loop finds its token cancelled.
        signalme.Delay.OnWait = wait => {
            if (wait != 2) { return; }

            queued = signalme.SetAsync(UserStatus.Available);
            signalme.Cancellation.Cancel();
        };

        Task<IntentOutcome> signal = signalme.PlayAsync(UserMood.Alerting);

        Assert.Equal(IntentOutcome.SignalInterrupted, await signal);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued!);
        Assert.Null(await signalme.StopAsync());
        Assert.Equal(UserStatus.Busy, signalme.Statuses.Store.Get());
    }

    /// <summary>
    ///     The read may complete with a message just before the stop comes in, so that the loop looks at it
    ///     with its token already cancelled. It is then treated like a message found in the drain: cancelled,
    ///     never applied.
    /// </summary>
    /// <remarks>
    ///     The callback is registered after the loop parked in its read, so on Cancel it runs before the
    ///     read's own registration: the read completes with the intent while the token is already cancelled,
    ///     the window a mode sending an intent at the moment of Ctrl+C falls into. Were the callbacks ever
    ///     run the other way round, the read would be cancelled and the intent drained: cancelled either way.
    /// </remarks>
    [Fact]
    public async Task An_intent_read_just_before_the_stop_is_cancelled_not_applied() {
        await using Harness signalme = new(UserStatus.Busy);
        Task<IntentOutcome>? queued = null;
        using CancellationTokenRegistration registration = signalme.Cancellation.Token.Register(() => queued = signalme.SetAsync(UserStatus.Available));

        Assert.Null(await signalme.StopAsync());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued!);
        Assert.Equal(UserStatus.Busy, signalme.Statuses.Store.Get());
        Assert.Equal([Busy], signalme.Device.Commands);
        Assert.DoesNotContain("Status: available", signalme.Console.Output);
    }

    /// <summary>
    ///     Same window, with a signal: the animation must not start after the stop, or its "Playing:" line
    ///     would land between "Stopping SignalMe..." and "SignalMe stopped." and its frames on the LEDs.
    /// </summary>
    [Fact]
    public async Task A_signal_read_just_before_the_stop_is_not_started() {
        await using Harness signalme = new(UserStatus.Busy);
        Task<IntentOutcome>? queued = null;
        using CancellationTokenRegistration registration = signalme.Cancellation.Token.Register(() => queued = signalme.PlayAsync(UserMood.Happy));

        Assert.Null(await signalme.StopAsync());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued!);
        Assert.Equal([Busy], signalme.Device.Commands);
        Assert.DoesNotContain("Playing: happy", signalme.Console.Output);
        Assert.Equal(0, signalme.Delay.Waits);
        Assert.Null(signalme.State.PlayingSignal);
    }

    [Fact]
    public async Task An_intent_posted_after_the_loop_stopped_is_refused() {
        await using Harness signalme = new(UserStatus.Busy);
        await signalme.StopAsync();

        OperationCanceledException thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => signalme.SetAsync(UserStatus.Available));

        Assert.Equal("SignalMe is stopping.", thrown.Message);
    }

    /// <summary>
    ///     "The coordinator has stopped" covers a loop that died of the device as much as one that was
    ///     cancelled: an intent queued behind the fault gets the fault, a later one is simply refused.
    /// </summary>
    [Fact]
    public async Task An_intent_posted_after_the_loop_faulted_is_refused() {
        await using Harness signalme = new(UserStatus.Busy);
        signalme.Device.RefuseFromCall = 2;
        await Assert.ThrowsAsync<DeviceCommandFailedException>(() => signalme.SetAsync(UserStatus.Available));
        Assert.IsType<DeviceCommandFailedException>(await signalme.StopAsync());

        OperationCanceledException thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => signalme.SetAsync(UserStatus.DoNotDisturb));

        Assert.Equal("SignalMe is stopping.", thrown.Message);
    }

    [Fact]
    public async Task A_device_refusal_faults_the_loop_and_is_reported_once() {
        await using Harness signalme = new(UserStatus.Busy);
        signalme.Device.RefuseFromCall = 2;

        DeviceCommandFailedException fromIntent = await Assert.ThrowsAsync<DeviceCommandFailedException>(() => signalme.SetAsync(UserStatus.Available));
        Exception?                   fromLoop   = await signalme.StopAsync();

        Assert.Same(fromIntent, fromLoop);
        Assert.Equal(["The Luxafor device refused to display the 'available' status."], signalme.Console.Error);
    }

    [Fact]
    public async Task A_device_failure_during_a_signal_faults_the_loop_and_the_signal_alike() {
        await using Harness signalme = new(UserStatus.Busy);
        signalme.Device.ThrowOnCall = 4;

        DeviceCommandFailedException fromSignal = await Assert.ThrowsAsync<DeviceCommandFailedException>(() => signalme.PlayAsync(UserMood.Alerting));
        Exception?                   fromLoop   = await signalme.StopAsync();

        Assert.Same(fromSignal, fromLoop);
        Assert.IsType<InvalidOperationException>(fromSignal.InnerException);
        Assert.Single(signalme.Console.Error);
    }

    [Fact]
    public async Task An_intent_queued_behind_a_device_fault_completes_with_that_fault() {
        await using Harness signalme = new(UserStatus.Busy);
        Task<IntentOutcome>? queued = null;
        // Queued from inside the animation, right before the device breaks on the next frame.
        signalme.Delay.OnWait = wait => {
            if (wait != 2) { return; }

            signalme.Device.RefuseFromCall = signalme.Device.Commands.Count + 1;
            queued = signalme.SetAsync(UserStatus.Available);
        };

        DeviceCommandFailedException fromSignal = await Assert.ThrowsAsync<DeviceCommandFailedException>(() => signalme.PlayAsync(UserMood.Alerting));
        DeviceCommandFailedException fromQueued = await Assert.ThrowsAsync<DeviceCommandFailedException>(() => queued!);

        Assert.Same(fromSignal, fromQueued);
        Assert.IsType<DeviceCommandFailedException>(await signalme.StopAsync());
        Assert.Single(signalme.Console.Error);
        Assert.Equal(UserStatus.Busy, signalme.Statuses.Store.Get());
    }

    [Fact]
    public async Task A_refused_restore_after_a_signal_faults_the_loop() {
        await using Harness signalme = new(UserStatus.Busy);
        signalme.Delay.OnWait = wait => {
            // Alerting asks for forty waits: after the last one the animation returns, and the next write
            // is the one that puts the status back.
            if (wait == 40) { signalme.Device.RefuseFromCall = signalme.Device.Commands.Count + 1; }
        };

        await Assert.ThrowsAsync<DeviceCommandFailedException>(() => signalme.PlayAsync(UserMood.Alerting));

        Assert.IsType<DeviceCommandFailedException>(await signalme.StopAsync());
        Assert.Equal(["The Luxafor device refused to display the 'busy' status."], signalme.Console.Error);
    }

    [Fact]
    public async Task Turning_off_quietly_reports_a_refusal_instead_of_throwing() {
        await using Harness signalme = new(UserStatus.Busy);
        await signalme.StopAsync();
        signalme.Device.RefuseFromCall = 2;

        signalme.Coordinator.TurnOffQuietly();

        Assert.Equal(["The Luxafor device refused to turn its LEDs off."], signalme.Console.Error);
    }

    [Fact]
    public async Task Turning_off_quietly_reports_a_broken_device_instead_of_throwing() {
        await using Harness signalme = new(UserStatus.Busy);
        await signalme.StopAsync();
        signalme.Device.ThrowOnCall = 2;

        signalme.Coordinator.TurnOffQuietly();

        Assert.Equal(["The Luxafor device could not be turned off: InvalidOperationException: USB write failed"], signalme.Console.Error);
    }

    #endregion

    /// <summary>
    ///     A coordinator over fakes, initialized and running, with the few moves every test makes.
    /// </summary>
    private sealed class Harness : IAsyncDisposable {

        public Harness(UserStatus? stored = null, SessionState session = SessionState.Active) {
            if (stored is not null) { Statuses.Store.Set(stored); }

            Coordinator = new StatusCoordinator("manual", Device, Statuses.Store, Console, Delay, Statuses.Store.Get());
            Coordinator.Initialize(session);
            Loop = Coordinator.RunAsync(Cancellation.Token);
        }

        public FakeLuxaforDevice       Device       { get; } = new();
        public TemporaryStatusStore    Statuses     { get; } = new();
        public FakeConsole             Console      { get; } = new();
        public ControllableDelay       Delay        { get; } = new();
        public CancellationTokenSource Cancellation { get; } = new();
        public StatusCoordinator       Coordinator  { get; }
        public Task                    Loop         { get; }

        public SignalMeState State => Coordinator.State;

        public Task<IntentOutcome> ExecuteAsync(SignalMeIntent intent) {
            return Coordinator.ExecuteAsync(intent, CancellationToken.None);
        }

        public Task<IntentOutcome> SetAsync(UserStatus status) {
            return ExecuteAsync(new SignalMeIntent.SetDesiredStatus(status));
        }

        public Task<IntentOutcome> PlayAsync(UserMood mood) {
            return ExecuteAsync(new SignalMeIntent.PlaySignal(mood));
        }

        /// <summary>Raises the lock and waits for the loop to have reported it.</summary>
        public Task LockAsync() {
            return ChangeSessionAsync(SessionState.Locked);
        }

        public Task UnlockAsync() {
            return ChangeSessionAsync(SessionState.Active);
        }

        /// <summary>Stops the loop and returns what it died of, if anything.</summary>
        public async Task<Exception?> StopAsync() {
            await Cancellation.CancelAsync();

            return await Record.ExceptionAsync(() => Loop);
        }

        public async ValueTask DisposeAsync() {
            // Awaited whatever its state: a loop that faulted without being awaited would otherwise leave
            // an unobserved exception behind.
            await StopAsync();

            Coordinator.Dispose();
            Cancellation.Dispose();
            Statuses.Dispose();
        }

        private async Task ChangeSessionAsync(SessionState state) {
            int reported = Console.Output.Count + 1;
            Coordinator.OnSessionChanged(state);
            await Console.WaitForOutputAsync(reported);
        }

    }

}
