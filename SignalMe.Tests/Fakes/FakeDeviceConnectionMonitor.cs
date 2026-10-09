using SignalMe.Devices;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     A device connection monitor the test drives by hand: the device is unplugged when the test says so.
/// </summary>
public sealed class FakeDeviceConnectionMonitor : IDeviceConnectionMonitor {

    /// <inheritdoc />
    public event EventHandler? Disconnected;

    public bool Started    { get; private set; }
    public bool IsDisposed { get; private set; }

    /// <summary>Whether anyone still listens: the runtime must let go of the monitor when it stops.</summary>
    public bool IsSubscribed => Disconnected is not null;

    /// <summary>Reports the device missing, the way the Luxafor library's lookup would.</summary>
    public void Disconnect() {
        Disconnected?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Start() {
        Started = true;
    }

    /// <inheritdoc />
    public void Dispose() {
        IsDisposed = true;
    }

}
