#region Usings declarations

using System;
using System.ComponentModel;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Converters;
using SignalMe.Infrastructure;
using SignalMe.Services;

using Spectre.Console.Cli;

#endregion

namespace SignalMe.Commands;

public class AsCommand : Command<AsCommand.Settings> {

    public override int Execute(CommandContext context, Settings settings) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        string statusOrMood = settings.Status.Trim().ToLowerInvariant();

        // Validated before a device is acquired: a typo is the user's problem, not the hardware's, and
        // reporting a missing device for "signalme as buys" points at the wrong thing entirely.
        if (!SignalMeService.IsKnown(statusOrMood)) {
            Console.Error.WriteLine($"Unknown status or mood: '{settings.Status}'.");
            Console.Error.WriteLine($"Statuses: {string.Join(", ", UserStatusConverter.KnownValues)}");
            Console.Error.WriteLine($"Moods:    {string.Join(", ", UserMoodConverter.KnownValues)}");

            return ExitCode.UsageError;
        }

        if (!LuxaforDeviceHelper.TryGetDefaultLuxaforDevice(out ILuxaforDevice? luxaforDevice)) { return ExitCode.DeviceError; }

        try {
            SignalMeService service = new(luxaforDevice);
            service.SetAs(statusOrMood);

            return ExitCode.Success;
        } finally {
            luxaforDevice.Dispose();
        }
    }

    #region Nested types declarations

    public class Settings : CommandSettings {

        [CommandArgument(0, "<status>")]
        [Description("Status : \r\n  - available (or free), busy, do-not-disturb (or dnd), away\r\n  - happy, bored, desperate, ready, warning, alerting")]
        public string Status { get; set; } = string.Empty;

    }

    #endregion

}
