#region Usings declarations

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Infrastructure;

public static class LuxaforDeviceHelper {

    #region Statics members declarations

    public static bool TryGetDefaultLuxaforDevice([NotNullWhen(true)] out ILuxaforDevice? device) {
        device = null;
        try {
            device = Luxafor.GetDevices().FirstOrDefault();
            if (device is not null) { return true; }

            Console.Error.WriteLine("No Luxafor device detected. Check that the device is plugged into a USB port.");

            return false;
        } catch (Exception exception) {
            device?.Dispose();
            device = null;

            // Reporting the cause matters more than hiding it: without this the CLI used to exit in error
            // without printing anything at all, whatever went wrong.
            Console.Error.WriteLine($"Could not reach a Luxafor device: {exception.GetType().Name}: {exception.Message}");
            Console.Error.WriteLine("The device may be unplugged or already held by another application. Luxafor devices are driven through the Windows HID stack and cannot be reached on other systems.");

            return false;
        }
    }

    #endregion

}
