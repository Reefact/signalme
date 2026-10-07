using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     A refused command must never pass for a success, whichever device call was refused; a device that
///     throws is a device failure too, with the cause kept inside, except a cancellation, which is nobody's
///     failure.
/// </summary>
public sealed class DeviceCommandPolicyTests {

    [Fact]
    public void SetColorOrThrow_passes_the_color_through_when_the_device_accepts() {
        FakeLuxaforDevice device = new();

        device.SetColorOrThrow(BrightColor.Red);

        Assert.Equal("SetColor(#FF0000)", device.LastCommand);
    }

    [Fact]
    public void SetColorOrThrow_throws_when_the_device_refuses() {
        FakeLuxaforDevice device = new() { RefuseFromCall = 1 };

        DeviceCommandFailedException exception = Assert.Throws<DeviceCommandFailedException>(() => device.SetColorOrThrow(BrightColor.Red));

        Assert.Contains("#FF0000", exception.Message);
    }

    [Fact]
    public void SetColorOrThrow_names_the_operation_when_the_caller_provides_one() {
        FakeLuxaforDevice device = new() { RefuseFromCall = 1 };

        DeviceCommandFailedException exception = Assert.Throws<DeviceCommandFailedException>(() => device.SetColorOrThrow(BrightColor.Red, "display the 'Busy' status"));

        Assert.Equal("The Luxafor device refused to display the 'Busy' status.", exception.Message);
    }

    [Fact]
    public void TurnOffOrThrow_returns_quietly_when_the_device_accepts() {
        FakeLuxaforDevice device = new();

        device.TurnOffOrThrow();

        Assert.Equal("TurnOff", device.LastCommand);
    }

    [Fact]
    public void TurnOffOrThrow_throws_when_the_device_refuses() {
        FakeLuxaforDevice device = new() { RefuseFromCall = 1 };

        DeviceCommandFailedException exception = Assert.Throws<DeviceCommandFailedException>(() => device.TurnOffOrThrow());

        Assert.Equal("The Luxafor device refused to turn its LEDs off.", exception.Message);
    }

    [Fact]
    public void SendOrThrow_forwards_the_command_when_the_device_accepts() {
        FakeLuxaforDevice device  = new();
        LightingCommand   command = LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(3), BrightColor.Green);

        device.SendOrThrow(command);

        Assert.Equal($"Send({command})", device.LastCommand);
    }

    [Fact]
    public void SendOrThrow_throws_and_names_the_refused_command() {
        FakeLuxaforDevice device  = new() { RefuseFromCall = 1 };
        LightingCommand   command = LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(3), BrightColor.Green);

        DeviceCommandFailedException exception = Assert.Throws<DeviceCommandFailedException>(() => device.SendOrThrow(command));

        Assert.Contains(command.ToString(), exception.Message);
    }

    [Fact]
    public void A_device_exception_is_wrapped_with_the_cause_inside() {
        FakeLuxaforDevice device = new() { ThrowOnCall = 1 };

        DeviceCommandFailedException exception = Assert.Throws<DeviceCommandFailedException>(() => device.SetColorOrThrow(BrightColor.Red));

        Assert.Equal("The Luxafor device could not be reached: InvalidOperationException: USB write failed", exception.Message);
        Assert.Same(device.Failure, exception.InnerException);
    }

    /// <summary>
    ///     A cancellation coming out of the device call is the caller's token at work, not the device
    ///     failing: wrapped, it would stop a run with a device error where the user merely pressed Ctrl+C.
    /// </summary>
    [Fact]
    public void A_cancellation_thrown_by_the_device_is_not_a_device_failure() {
        FakeLuxaforDevice device = new() { ThrowOnCall = 1, Failure = new OperationCanceledException() };

        Assert.Throws<OperationCanceledException>(() => device.SetColorOrThrow(BrightColor.Red));
    }

}
