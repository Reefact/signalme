#region Usings declarations

using System.Collections.Generic;
using System.Linq;

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Devices;

/// <summary>
///     The real discovery, through the Luxafor controller library.
/// </summary>
public sealed class LuxaforDeviceDiscovery : ILuxaforDeviceDiscovery {

    #region Statics members declarations

    public static LuxaforDeviceDiscovery Instance { get; } = new();

    #endregion

    /// <inheritdoc />
    public IReadOnlyList<ILuxaforDevice> Discover() {
        // Materialised at once: the library hands back a lazy enumeration, and the USB ports must be
        // scanned exactly once, at start-up, not again each time the list is counted or indexed.
        return Luxafor.GetDevices().ToArray();
    }

}
