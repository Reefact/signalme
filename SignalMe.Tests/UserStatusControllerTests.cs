using SignalMe.Infrastructure;
using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

public sealed class UserStatusControllerTests {

    [Theory]
    [InlineData(UserStatus.Available, "#00FF00")]
    [InlineData(UserStatus.Busy, "#FFFF00")]
    [InlineData(UserStatus.DoNotDisturb, "#FF0000")]
    [InlineData(UserStatus.Away, "#9932CC")]
    public void Display_lights_the_color_of_the_status(UserStatus status, string expectedColor) {
        using TemporaryStatusStore statuses = new();
        FakeLuxaforDevice          device   = new();

        new UserStatusController(device, statuses.Store).Display(status);

        Assert.Equal($"SetColor({expectedColor})", device.LastCommand);
    }

    [Fact]
    public void Display_remembers_the_status() {
        using TemporaryStatusStore statuses = new();
        FakeLuxaforDevice          device   = new();

        new UserStatusController(device, statuses.Store).Display(UserStatus.Away);

        Assert.Equal(UserStatus.Away, statuses.Store.Get());
    }

    [Fact]
    public void Display_turns_the_device_off_when_there_is_no_status() {
        using TemporaryStatusStore statuses = new();
        FakeLuxaforDevice          device   = new();

        new UserStatusController(device, statuses.Store).Display(null);

        Assert.Equal("TurnOff", device.LastCommand);
    }

    [Fact]
    public void Display_throws_when_the_device_refuses_the_color() {
        using TemporaryStatusStore statuses = new();
        FakeLuxaforDevice          device   = new() { RefuseFromCall = 1 };

        Assert.Throws<DeviceCommandFailedException>(() => new UserStatusController(device, statuses.Store).Display(UserStatus.Busy));
    }

    [Fact]
    public void Display_does_not_remember_a_status_the_device_refused_to_show() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Available);
        FakeLuxaforDevice device = new() { RefuseFromCall = 1 };

        Assert.Throws<DeviceCommandFailedException>(() => new UserStatusController(device, statuses.Store).Display(UserStatus.DoNotDisturb));

        // The remembered status must keep describing what the LEDs are actually showing.
        Assert.Equal(UserStatus.Available, statuses.Store.Get());
    }

    [Fact]
    public void Display_throws_when_the_device_refuses_to_turn_off() {
        using TemporaryStatusStore statuses = new();
        FakeLuxaforDevice          device   = new() { RefuseFromCall = 1 };

        Assert.Throws<DeviceCommandFailedException>(() => new UserStatusController(device, statuses.Store).Display(null));
    }

    [Fact]
    public void GetUserStatusColor_is_black_when_no_status_is_set() {
        using TemporaryStatusStore statuses = new();

        Assert.Equal("#000000", UserStatusController.GetUserStatusColor(null).ToString());
    }

    [Fact]
    public void TurnOff_forgets_the_status_only_once_the_device_has_obeyed() {
        using TemporaryStatusStore statuses = new();
        statuses.Store.Set(UserStatus.Busy);
        FakeLuxaforDevice device = new() { RefuseFromCall = 1 };

        Assert.Throws<DeviceCommandFailedException>(() => new SignalMeService(device, statuses.Store).TurnOff());
        Assert.Equal(UserStatus.Busy, statuses.Store.Get());

        new SignalMeService(new FakeLuxaforDevice(), statuses.Store).TurnOff();
        Assert.Null(statuses.Store.Get());
    }

}
