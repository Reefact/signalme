using SignalMe.Infrastructure;
using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     Ctrl+C must stop an animation quickly and still put the durable status back.
/// </summary>
public sealed class CancellationTests {

    /// <summary>Derived from the enum, so adding a mood cannot silently leave it untested.</summary>
    public static TheoryData<UserMood> AllMoods => new(Enum.GetValues<UserMood>());

    [Theory]
    [MemberData(nameof(AllMoods))]
    public async Task An_interrupted_animation_restores_the_previous_status(UserMood mood) {
        using TemporaryStatusStore      statuses     = new();
        statuses.Store.Set(UserStatus.DoNotDisturb);
        using CancellationTokenSource   cancellation = new();
        FakeLuxaforDevice               device       = new();
        // Stands in for the user hitting Ctrl+C a few frames in.
        InstantDelay delay = new() { OnWait = wait => { if (wait == 3) { cancellation.Cancel(); } } };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new UserMoodLedController(device, statuses.Store, delay).DisplayAsync(mood, cancellation.Token));

        Assert.Equal("SetColor(#FF0000)", device.LastCommand);
        Assert.Equal(UserStatus.DoNotDisturb, statuses.Store.Get());
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public async Task An_interrupted_animation_stops_early(UserMood mood) {
        using TemporaryStatusStore    statuses     = new();
        using CancellationTokenSource cancellation = new();
        FakeLuxaforDevice             device       = new();
        InstantDelay                  delay        = new() { OnWait = wait => { if (wait == 3) { cancellation.Cancel(); } } };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new UserMoodLedController(device, statuses.Store, delay).DisplayAsync(mood, cancellation.Token));

        // It gave up at the third wait rather than running the sequence to the end.
        Assert.Equal(3, delay.Waits);
    }

    [Fact]
    public async Task An_animation_cancelled_before_it_starts_leaves_the_device_off_when_there_was_no_status() {
        using TemporaryStatusStore    statuses     = new();
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FakeLuxaforDevice device = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new UserMoodLedController(device, statuses.Store, new InstantDelay()).DisplayAsync(UserMood.Alerting, cancellation.Token));

        Assert.Equal("TurnOff", device.LastCommand);
    }

    [Fact]
    public async Task A_restore_that_fails_after_an_interruption_is_reported_without_hiding_the_interruption() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        using CancellationTokenSource cancellation = new();
        // Refuses everything from the moment the user interrupts, so the restore cannot go through either.
        FakeLuxaforDevice device = new();
        InstantDelay delay = new() {
            OnWait = wait => {
                if (wait != 3) { return; }

                device.RefuseFromCall = device.Commands.Count + 1;
                cancellation.Cancel();
            }
        };

        Exception? thrown = null;
        string error = await StandardError.CaptureAsync(async () =>
                                                            thrown = await Record.ExceptionAsync(
                                                                () => new UserMoodLedController(device, statuses.Store, delay).DisplayAsync(UserMood.Alerting, cancellation.Token)));

        Assert.IsAssignableFrom<OperationCanceledException>(thrown);
        Assert.Contains("could not be restored", error);
    }

}
