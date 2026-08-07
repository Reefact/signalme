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
    public static TheoryData<UserMood> RestoringMoods => [UserMood.Happy, UserMood.Bored, UserMood.Desperate, UserMood.Warning, UserMood.Alerting];

    public static TheoryData<UserMood> AllMoods => [UserMood.Happy, UserMood.Bored, UserMood.Desperate, UserMood.Ready, UserMood.Warning, UserMood.Alerting];

    [Theory]
    [MemberData(nameof(RestoringMoods))]
    public void An_animation_puts_the_previous_status_back(UserMood mood) {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice device = new();

        new UserMoodLedController(device, statuses.Store).Display(mood);

        Assert.Equal("SetColor(#FFFF00)", device.LastCommand);
        Assert.Equal(UserStatus.Busy, statuses.Store.Get());
    }

    [Theory]
    [MemberData(nameof(RestoringMoods))]
    public void An_animation_leaves_the_device_off_when_there_was_no_status(UserMood mood) {
        using TemporaryStatusStore statuses = new();
        FakeLuxaforDevice          device   = new();

        new UserMoodLedController(device, statuses.Store).Display(mood);

        Assert.Equal("TurnOff", device.LastCommand);
        Assert.Null(statuses.Store.Get());
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public void A_refused_command_stops_the_animation(UserMood mood) {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice device = new() { RefuseFromCall = 1 };

        (_, Exception? thrown) = StandardError.Capture(() => new UserMoodLedController(device, statuses.Store).Display(mood));

        Assert.IsType<DeviceCommandFailedException>(thrown);
        // The animation gave up straight away rather than sending its whole sequence into the void.
        Assert.Empty(device.Commands);
    }

    [Theory]
    [MemberData(nameof(AllMoods))]
    public void An_animation_error_is_never_replaced_by_a_restore_failure(UserMood mood) {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        // The device breaks mid-animation, then refuses everything, so the restore fails too.
        FakeLuxaforDevice device = new() { ThrowOnCall = 3, RefuseFromCall = 4 };

        (string error, Exception? thrown) = StandardError.Capture(() => new UserMoodLedController(device, statuses.Store).Display(mood));

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal("USB write failed", thrown!.Message);
        Assert.Contains("could not be restored", error);
    }

    [Fact]
    public void Ready_ends_on_available_and_remembers_it() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice device = new();

        new UserMoodLedController(device, statuses.Store).Display(UserMood.Ready);

        Assert.Equal("SetColor(#00FF00)", device.LastCommand);
        Assert.Equal(UserStatus.Available, statuses.Store.Get());
    }

    [Fact]
    public void Ready_falls_back_to_the_previous_status_when_it_does_not_complete() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice device = new() { ThrowOnCall = 2 };

        Assert.Throws<InvalidOperationException>(() => new UserMoodLedController(device, statuses.Store).Display(UserMood.Ready));

        // It never reached "available", so the user is still busy.
        Assert.Equal("SetColor(#FFFF00)", device.LastCommand);
        Assert.Equal(UserStatus.Busy, statuses.Store.Get());
    }

}
