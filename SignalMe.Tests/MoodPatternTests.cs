using SignalMe.Infrastructure;
using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     Checks the contract every animation owes, not the frames it draws: a refused command stops it, the
///     durable status comes back, and an animation's own error is never replaced by a restore failure.
/// </summary>
public sealed class MoodPatternTests {

    /// <summary>Every mood except "ready", which deliberately ends on another status.</summary>
    public static TheoryData<UserMood> RestoringMoods => new(Enum.GetValues<UserMood>().Where(mood => mood != UserMood.Ready));

    /// <summary>Derived from the enum, so adding a mood cannot silently leave it untested.</summary>
    public static TheoryData<UserMood> AllMoods => new(Enum.GetValues<UserMood>());

    private static Task PlayAsync(UserMood mood, FakeLuxaforDevice device, TemporaryStatusStore statuses, IDelay? delay = null, CancellationToken cancellationToken = default) {
        return new UserMoodLedController(device, statuses.Store, delay ?? new InstantDelay()).DisplayAsync(mood, cancellationToken);
    }

    [Theory]
    [MemberData(nameof(RestoringMoods))]
    public async Task An_animation_puts_the_previous_status_back(UserMood mood) {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice device = new();

        await PlayAsync(mood, device, statuses);

        Assert.Equal("SetColor(#FFFF00)", device.LastCommand);
        Assert.Equal(UserStatus.Busy, statuses.Store.Get());
    }

    [Theory]
    [MemberData(nameof(RestoringMoods))]
    public async Task An_animation_leaves_the_device_off_when_there_was_no_status(UserMood mood) {
        using TemporaryStatusStore statuses = new();
        FakeLuxaforDevice          device   = new();

        await PlayAsync(mood, device, statuses);

        Assert.Equal("TurnOff", device.LastCommand);
        Assert.Null(statuses.Store.Get());
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public async Task A_refused_command_stops_the_animation(UserMood mood) {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice device = new() { RefuseFromCall = 1 };

        await Assert.ThrowsAsync<DeviceCommandFailedException>(() => PlayAsync(mood, device, statuses));

        // The animation gave up straight away rather than sending its whole sequence into the void.
        Assert.Empty(device.Commands);
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public async Task An_animation_error_is_never_replaced_by_a_restore_failure(UserMood mood) {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        // The device breaks mid-animation, then refuses everything, so the restore fails too.
        FakeLuxaforDevice device = new() { ThrowOnCall = 3, RefuseFromCall = 4 };
        Exception?        thrown = null;

        string error = await StandardError.CaptureAsync(async () => thrown = await Record.ExceptionAsync(() => PlayAsync(mood, device, statuses)));

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal("USB write failed", thrown!.Message);
        Assert.Contains("could not be restored", error);
    }

    [Fact]
    public async Task Ready_ends_on_available_and_remembers_it() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice device = new();

        await PlayAsync(UserMood.Ready, device, statuses);

        Assert.Equal("SetColor(#00FF00)", device.LastCommand);
        Assert.Equal(UserStatus.Available, statuses.Store.Get());
    }

    [Fact]
    public async Task Ready_falls_back_to_the_previous_status_when_it_does_not_complete() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice device = new() { ThrowOnCall = 2 };

        await Assert.ThrowsAsync<InvalidOperationException>(() => PlayAsync(UserMood.Ready, device, statuses));

        // It never reached "available", so the user is still busy.
        Assert.Equal("SetColor(#FFFF00)", device.LastCommand);
        Assert.Equal(UserStatus.Busy, statuses.Store.Get());
    }

    [Fact]
    public async Task The_whole_suite_of_animations_runs_without_waiting_for_real() {
        using TemporaryStatusStore statuses = new();
        InstantDelay               delay    = new();

        foreach (UserMood mood in Enum.GetValues<UserMood>()) {
            await PlayAsync(mood, new FakeLuxaforDevice(), statuses, delay);
        }

        // Proof the animations really are asking to wait, and that the tests are simply not sitting there.
        Assert.True(delay.Waits > 100, $"expected the animations to request many waits, got {delay.Waits}.");
    }

}
