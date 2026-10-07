#region Usings declarations

using System.Collections.Generic;

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Devices;

/// <summary>
///     Finds the Luxafor devices plugged into the machine.
/// </summary>
/// <remarks>
///     The library's <c>Luxafor.GetDevices()</c> is a static call into the HID stack, which the tests cannot
///     run without hardware; this seam lets them hand the command line a list of fakes, or a failure.
/// </remarks>
public interface ILuxaforDeviceDiscovery {

    /// <summary>
    ///     Every device found, opened and ready to be driven. May throw: the caller reports the failure and
    ///     exits with a device error, since nothing can be shown without a device.
    /// </summary>
    IReadOnlyList<ILuxaforDevice> Discover();

}
