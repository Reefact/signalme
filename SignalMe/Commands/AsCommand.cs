#region Usings declarations

using System;
using System.ComponentModel;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Services;

using Spectre.Console.Cli;

#endregion

namespace SignalMe.Commands;

public class AsCommand : Command<AsCommand.Settings> {

    public override int Execute(CommandContext context, Settings settings) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        if (!LuxaforDeviceHelper.TryGetDefaultLuxaforDevice(out LuxaforDevice? luxaforDevice)) { return 1; }

        try {
            SignalMeService service = new(luxaforDevice);
            service.SetAs(settings.Status.ToLowerInvariant());

            return 0;
        } finally {
            luxaforDevice.Dispose();
        }
    }

    #region Nested types declarations

    public class Settings : CommandSettings {

        [CommandArgument(0, "<status>")]
        [Description("Status : \r\n  - available, busy, do-not-disturb (or dnd), away\r\n  - happy, bored, desperate, ready, warning, alerting")]
        public string Status { get; set; } = string.Empty;

    }

    #endregion

}