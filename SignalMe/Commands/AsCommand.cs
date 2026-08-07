#region Usings declarations

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Converters;
using SignalMe.Infrastructure;
using SignalMe.Services;

using Spectre.Console.Cli;

#endregion

namespace SignalMe.Commands;

public sealed class AsCommand : AsyncCommand<AsCommand.Settings> {

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        string statusOrMood = settings.Status.Trim().ToLowerInvariant();

        // Validated before a device is acquired: a typo is the user's problem, not the hardware's, and
        // reporting a missing device for "signalme as buys" points at the wrong thing entirely.
        if (!SignalMeService.IsKnown(statusOrMood)) {
            Console.Error.WriteLine($"Unknown status or mood: '{settings.Status}'.");
            Console.Error.WriteLine($"Durable statuses:  {string.Join(", ", UserStatusConverter.KnownValues)}");
            Console.Error.WriteLine($"Temporary signals: {string.Join(", ", UserMoodConverter.KnownValues)}");

            return ExitCode.UsageError;
        }

        if (!LuxaforDeviceHelper.TryGetDefaultLuxaforDevice(out ILuxaforDevice? luxaforDevice)) { return ExitCode.DeviceError; }

        using CancellationTokenSource cancellation = ConsoleCancellation.OnCtrlC();
        try {
            await new SignalMeService(luxaforDevice).SetAsAsync(statusOrMood, cancellation.Token).ConfigureAwait(false);

            return ExitCode.Success;
        } finally {
            luxaforDevice.Dispose();
        }
    }

    #region Nested types declarations

    public sealed class Settings : CommandSettings {

        [CommandArgument(0, "<status-or-signal>")]
        [Description("""
                     Durable status:    available (or free), busy, away, do-not-disturb (or dnd)
                     Temporary signal:  happy, bored, desperate, ready, warning, alerting

                     A temporary signal plays an animation and then restores the durable status,
                     except 'ready', which announces availability and ends on 'available'.
                     """)]
        public string Status { get; set; } = string.Empty;

    }

    #endregion

}
