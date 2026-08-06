using SignalMe.Infrastructure;
using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     The four outcomes of running a temporary animation over a durable status.
/// </summary>
/// <remarks>
///     Tested on <see cref="UserStatusController.PlayAndRestore(UserStatus?, Action)" /> itself rather than
///     through the animations, because the policy is what has to be right; the animations only have to use
///     it. <see cref="MoodPatternTests" /> checks that each of them does.
/// </remarks>
public sealed class RestorationPolicyTests {

    [Fact]
    public void Animation_succeeds_and_restore_succeeds_the_call_returns_and_the_status_is_back() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice    device     = new();
        UserStatusController controller = new(device, statuses.Store);

        (string error, Exception? thrown) = StandardError.Capture(() => controller.PlayAndRestore(UserStatus.Busy, () => { device.SetColorOrThrow(Reefact.LuxaforLightingDeviceController.BrightColor.White); }));

        Assert.Null(thrown);
        Assert.Empty(error);
        Assert.Equal("SetColor(#FFFF00)", device.LastCommand);
        Assert.Equal(UserStatus.Busy, statuses.Store.Get());
    }

    [Fact]
    public void Animation_succeeds_but_restore_fails_the_command_fails() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice    device     = new();
        UserStatusController controller = new(device, statuses.Store);

        // The animation runs fine, then the device stops answering just before the restore.
        DeviceCommandFailedException exception = Assert.Throws<DeviceCommandFailedException>(
            () => controller.PlayAndRestore(UserStatus.Busy, () => { device.RefuseFromCall = 1; }));

        // Leaving the LEDs on an animation color is a failure, not a success with a warning.
        Assert.Contains("Busy", exception.Message);
    }

    [Fact]
    public void Animation_fails_and_restore_succeeds_the_animation_error_is_the_one_that_surfaces() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice    device     = new();
        UserStatusController controller = new(device, statuses.Store);

        (string error, Exception? thrown) = StandardError.Capture(
            () => controller.PlayAndRestore(UserStatus.Busy, () => throw new InvalidOperationException("USB write failed")));

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal("USB write failed", thrown!.Message);
        Assert.Empty(error);
        Assert.Equal("SetColor(#FFFF00)", device.LastCommand);
    }

    [Fact]
    public void Animation_fails_and_restore_fails_too_the_animation_error_still_wins_and_the_restore_is_reported() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice    device     = new();
        UserStatusController controller = new(device, statuses.Store);

        (string error, Exception? thrown) = StandardError.Capture(() => controller.PlayAndRestore(UserStatus.Busy, () => {
            device.RefuseFromCall = 1;

            throw new InvalidOperationException("USB write failed");
        }));

        // The original cause must not be buried under the restore failure...
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal("USB write failed", thrown!.Message);
        // ... but the restore failure must not vanish either.
        Assert.Contains("could not be restored", error);
    }

    [Fact]
    public void Restoring_no_status_turns_the_device_off() {
        using TemporaryStatusStore statuses = new();
        FakeLuxaforDevice          device   = new();

        new UserStatusController(device, statuses.Store).PlayAndRestore(null, () => { });

        Assert.Equal("TurnOff", device.LastCommand);
    }

    [Fact]
    public void An_animation_may_end_on_another_status_than_the_one_it_started_from() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.DoNotDisturb);
        FakeLuxaforDevice device = new();

        new UserStatusController(device, statuses.Store).PlayAndRestore(UserStatus.DoNotDisturb, () => UserStatus.Available);

        Assert.Equal("SetColor(#00FF00)", device.LastCommand);
        Assert.Equal(UserStatus.Available, statuses.Store.Get());
    }

    [Fact]
    public void An_animation_that_fails_falls_back_to_the_status_it_started_from() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.DoNotDisturb);
        FakeLuxaforDevice device = new();

        Assert.Throws<InvalidOperationException>(() => new UserStatusController(device, statuses.Store)
                                                     .PlayAndRestore(UserStatus.DoNotDisturb, () => throw new InvalidOperationException("boom")));

        Assert.Equal("SetColor(#FF0000)", device.LastCommand);
        Assert.Equal(UserStatus.DoNotDisturb, statuses.Store.Get());
    }

}
