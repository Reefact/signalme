#region Usings declarations

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Infrastructure;

public static class LuxaforDeviceHelper {

    #region Statics members declarations

    public static bool TryGetDefaultLuxaforDevice([NotNullWhen(true)] out LuxaforDevice? device) {
        device = null;
        try {
            device = Luxafor.GetDevices().FirstOrDefault();
            if (device is not null) { return true; }

            Console.Error.WriteLine("No Luxafor device detected.");

            return false;
        } catch {
            device?.Dispose();
            device = null;

            return false;
        }
    }

    #endregion

}