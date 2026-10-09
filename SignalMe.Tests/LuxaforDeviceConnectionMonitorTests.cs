using SignalMe.Devices;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     The periodic check that the device is still plugged in, over a device or a check the test
///     controls: the real answer comes from the Windows HID stack.
/// </summary>
/// <remarks>
///     The interval is a few milliseconds so the checks come fast; every wait for an outcome is bounded,
///     so that a bug fails the test instead of hanging it.
/// </remarks>
public sealed class LuxaforDeviceConnectionMonitorTests {

    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(5);
    private static readonly TimeSpan Bound    = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task A_device_found_missing_is_reported_once_and_no_longer_checked() {
        int checks = 0;
        int raised = 0;
        TaskCompletionSource disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using LuxaforDeviceConnectionMonitor monitor = new(() => Interlocked.Increment(ref checks) < 3, Interval);
        monitor.Disconnected += (_, _) => {
            Interlocked.Increment(ref raised);
            disconnected.TrySetResult();
        };

        monitor.Start();
        await disconnected.Task.WaitAsync(Bound);
        // Long enough for several more ticks, had the watch gone on.
        await Task.Delay(Interval * 10);

        Assert.Equal(1, Volatile.Read(ref raised));
        Assert.Equal(3, Volatile.Read(ref checks));
    }

    [Fact]
    public async Task A_device_unplugged_is_seen_through_its_own_connection_state_without_a_write() {
        FakeLuxaforDevice    device       = new();
        TaskCompletionSource disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using LuxaforDeviceConnectionMonitor monitor = new(device, Interval);
        monitor.Disconnected += (_, _) => disconnected.TrySetResult();

        monitor.Start();
        device.IsConnected = false;
        await disconnected.Task.WaitAsync(Bound);

        Assert.Empty(device.Commands);
    }

    [Fact]
    public async Task A_check_that_throws_is_not_taken_for_a_disconnection() {
        int checks = 0;
        TaskCompletionSource disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using LuxaforDeviceConnectionMonitor monitor = new(() => Interlocked.Increment(ref checks) switch {
            1 => throw new InvalidOperationException("USB stack unavailable"),
            2 => true,
            _ => false
        }, Interval);
        monitor.Disconnected += (_, _) => disconnected.TrySetResult();

        monitor.Start();
        await disconnected.Task.WaitAsync(Bound);

        // The watch survived the failed check and went on until the device was really found missing.
        Assert.Equal(3, Volatile.Read(ref checks));
    }

    [Fact]
    public async Task Nothing_is_reported_once_disposed() {
        bool connected = true;
        bool raised    = false;
        TaskCompletionSource firstCheck = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LuxaforDeviceConnectionMonitor monitor = new(() => {
            firstCheck.TrySetResult();

            return Volatile.Read(ref connected);
        }, Interval);
        monitor.Disconnected += (_, _) => Volatile.Write(ref raised, true);

        monitor.Start();
        await firstCheck.Task.WaitAsync(Bound);
        monitor.Dispose();
        // Unplugged right after the stop: a check still in flight sees it, and must keep it to itself.
        Volatile.Write(ref connected, false);
        await Task.Delay(Interval * 10);

        Assert.False(Volatile.Read(ref raised));
    }

    [Fact]
    public void A_disposed_monitor_cannot_be_started() {
        LuxaforDeviceConnectionMonitor monitor = new(() => true, Interval);
        monitor.Dispose();
        monitor.Dispose();

        Assert.Throws<ObjectDisposedException>(monitor.Start);
    }

    [Fact]
    public void A_monitor_cannot_be_started_twice() {
        using LuxaforDeviceConnectionMonitor monitor = new(() => true, Bound);
        monitor.Start();

        Assert.Throws<InvalidOperationException>(monitor.Start);
    }

}
