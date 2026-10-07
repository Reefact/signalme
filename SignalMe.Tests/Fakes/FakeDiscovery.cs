using Reefact.LuxaforLightingDeviceController;

using SignalMe.Devices;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     A discovery that finds the devices it was given, or fails the way the HID stack does when it cannot
///     be reached.
/// </summary>
public sealed class FakeDiscovery : ILuxaforDeviceDiscovery {

    private readonly ILuxaforDevice[] _devices;

    public FakeDiscovery(params ILuxaforDevice[] devices) {
        _devices = devices;
    }

    /// <summary>Thrown by <see cref="Discover" /> instead of returning the devices. Null never throws.</summary>
    public Exception? Failure { get; set; }

    /// <summary>How many times the ports were scanned: the command line must do it once, and never for help.</summary>
    public int Calls { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<ILuxaforDevice> Discover() {
        Calls++;
        if (Failure is not null) { throw Failure; }

        return _devices;
    }

}
