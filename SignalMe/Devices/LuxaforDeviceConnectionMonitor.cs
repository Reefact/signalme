#region Usings declarations

using System;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Devices;

/// <summary>
///     Asks the Luxafor device, at a fixed interval, whether it is still plugged in.
/// </summary>
/// <remarks>
///     <para>
///         The check is <see cref="ILuxaforDevice.IsConnected" />, which asks Windows whether the device
///         path is still among the HID devices present. It opens nothing, writes nothing and does not go
///         through the handle signalme writes with, so polling it from this monitor's thread leaves the
///         device with a single writer, the coordinator.
///     </para>
///     <para>
///         A device unplugged and plugged back within one interval is not seen: its path is present at
///         every check. Its old handle is dead all the same, so the next write fails and stops SignalMe
///         as it did before this watch existed.
///     </para>
/// </remarks>
public sealed class LuxaforDeviceConnectionMonitor : IDeviceConnectionMonitor {

    #region Statics members declarations

    /// <summary>
    ///     Often enough that an unplugged device stops SignalMe before anyone relies on what it claims to
    ///     show, rarely enough that asking Windows costs nothing worth measuring.
    /// </summary>
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(2);

    private static Func<bool> ConnectionOf(ILuxaforDevice device) {
        ArgumentNullException.ThrowIfNull(device);

        return () => device.IsConnected;
    }

    #endregion

    #region Fields declarations

    private readonly Func<bool>    _isConnected;
    private readonly PeriodicTimer _timer;
    private volatile bool          _disposed;
    private          bool          _started;

    #endregion

    #region Constructors declarations

    /// <summary>Watches <paramref name="device" />, every <see cref="DefaultInterval" />.</summary>
    public LuxaforDeviceConnectionMonitor(ILuxaforDevice device) : this(device, DefaultInterval) { }

    /// <summary>Watches <paramref name="device" />, every <paramref name="interval" />.</summary>
    public LuxaforDeviceConnectionMonitor(ILuxaforDevice device, TimeSpan interval) : this(ConnectionOf(device), interval) { }

    /// <param name="isConnected">The check itself; the tests replace the device's, which needs hardware.</param>
    /// <param name="interval">The time between two checks; the first one runs one interval after <see cref="Start" />.</param>
    public LuxaforDeviceConnectionMonitor(Func<bool> isConnected, TimeSpan interval) {
        ArgumentNullException.ThrowIfNull(isConnected);

        _isConnected = isConnected;
        _timer       = new PeriodicTimer(interval);
    }

    #endregion

    /// <inheritdoc />
    public event EventHandler? Disconnected;

    /// <inheritdoc />
    public void Start() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_started) { throw new InvalidOperationException("The device connection monitor is already started."); }

        _started = true;
        // Runs until the device is found missing or the timer is disposed; it cannot fault, since a check
        // that throws is caught and the only subscriber, the coordinator's queue, never throws.
        _ = WatchAsync();
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) { return; }

        _disposed = true;
        // Completes the pending wait with false, and every later one too: the watch ends on its own.
        _timer.Dispose();
    }

    private async Task WatchAsync() {
        while (await _timer.WaitForNextTickAsync().ConfigureAwait(false)) {
            if (IsConnected()) { continue; }

            // A check that was running while the runtime stopped must not report anything: the runtime
            // has unsubscribed and is releasing the device already.
            if (!_disposed) { Disconnected?.Invoke(this, EventArgs.Empty); }

            return;
        }
    }

    /// <summary>
    ///     A check that throws says nothing about the device, so it is taken as still connected: stopping
    ///     SignalMe on a failed enumeration would turn a glitch of the USB stack into a lost presence. The
    ///     next check, or the next write, tells the truth.
    /// </summary>
    private bool IsConnected() {
        try {
            return _isConnected();
        } catch (Exception) {
            return true;
        }
    }

}
