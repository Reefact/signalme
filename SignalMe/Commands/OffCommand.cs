#region Usings declarations

using System;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Services;

using Spectre.Console.Cli;

#endregion

namespace SignalMe.Commands;

public class OffCommand : Command {

    public override int Execute(CommandContext context) {
        ArgumentNullException.ThrowIfNull(context);

        if (!LuxaforDeviceHelper.TryGetDefaultLuxaforDevice(out LuxaforDevice? luxaforDevice)) { return 1; }

        try {
            SignalMeService service = new(luxaforDevice);
            service.TurnOff();

            return 0;
        } finally {
            luxaforDevice.Dispose();
        }
    }

}