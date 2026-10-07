using SignalMe.Devices;
using SignalMe.Infrastructure;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     The identification wave owes one thing above all: however it ends, the LEDs are off afterwards.
/// </summary>
public sealed class DeviceIdentificationWaveTests {

    private const int FramesPerWave = 12;

    private static string Frame(int led) {
        return $"Send(Set LED n° {led} color to #FFFFFF)";
    }

    [Fact]
    public async Task The_frames_light_the_six_leds_one_after_the_other_in_white_twice_and_end_with_the_leds_off() {
        FakeLuxaforDevice device = new();

        await DeviceIdentificationWave.PlayAsync(device, new InstantDelay(), CancellationToken.None);

        string[] expected = [.. Enumerable.Range(0, FramesPerWave).Select(frame => Frame(frame % 6 + 1)), "TurnOff"];
        Assert.Equal(expected, device.Commands);
    }

    [Fact]
    public async Task The_wave_waits_once_per_frame() {
        InstantDelay delay = new();

        await DeviceIdentificationWave.PlayAsync(new FakeLuxaforDevice(), delay, CancellationToken.None);

        Assert.Equal(FramesPerWave, delay.Waits);
    }

    [Fact]
    public async Task A_wave_cancelled_at_its_third_wait_stops_there_with_the_leds_off() {
        using CancellationTokenSource cancellation = new();
        FakeLuxaforDevice             device       = new();
        InstantDelay                  delay        = new() { OnWait = wait => { if (wait == 3) { cancellation.Cancel(); } } };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DeviceIdentificationWave.PlayAsync(device, delay, cancellation.Token));

        Assert.Equal([Frame(1), Frame(2), Frame(3), "TurnOff"], device.Commands);
        Assert.Equal(3, delay.Waits);
    }

    [Fact]
    public async Task A_wave_cancelled_before_it_starts_stops_at_its_first_wait_with_the_leds_off() {
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();
        FakeLuxaforDevice device = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DeviceIdentificationWave.PlayAsync(device, new InstantDelay(), cancellation.Token));

        Assert.Equal("TurnOff", device.LastCommand);
    }

    [Fact]
    public async Task A_broken_device_surfaces_as_a_device_failure_with_the_leds_off() {
        FakeLuxaforDevice device = new() { ThrowOnCall = 3 };

        DeviceCommandFailedException thrown = await Assert.ThrowsAsync<DeviceCommandFailedException>(() => DeviceIdentificationWave.PlayAsync(device, new InstantDelay(), CancellationToken.None));

        Assert.IsType<InvalidOperationException>(thrown.InnerException);
        // The broken frame is the third call; the clean-up that follows went through.
        Assert.Equal([Frame(1), Frame(2), "TurnOff"], device.Commands);
    }

    /// <summary>
    ///     A device refusing everything refuses the clean-up too; what matters is that the wave stopped at
    ///     the first refusal and reported it, rather than sending its whole sequence into the void.
    /// </summary>
    [Fact]
    public async Task A_refused_frame_stops_the_wave() {
        FakeLuxaforDevice device = new() { RefuseFromCall = 2 };

        await Assert.ThrowsAsync<DeviceCommandFailedException>(() => DeviceIdentificationWave.PlayAsync(device, new InstantDelay(), CancellationToken.None));

        Assert.Equal([Frame(1)], device.Commands);
    }

    [Fact]
    public async Task A_device_that_cannot_switch_off_after_a_complete_wave_is_a_device_failure() {
        FakeLuxaforDevice device = new() { RefuseFromCall = FramesPerWave + 1 };

        DeviceCommandFailedException thrown = await Assert.ThrowsAsync<DeviceCommandFailedException>(() => DeviceIdentificationWave.PlayAsync(device, new InstantDelay(), CancellationToken.None));

        Assert.Equal("The Luxafor device refused to turn its LEDs off.", thrown.Message);
        Assert.Equal(FramesPerWave, device.Commands.Count);
    }

}
