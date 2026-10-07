#region Usings declarations

using System;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;

#endregion

namespace SignalMe.Devices;

/// <summary>
///     The short white chase a device plays so that the user can tell which of several it is.
/// </summary>
/// <remarks>
///     Not a mood: it reads no status and leaves none behind. Whatever stops it, the LEDs are turned off
///     before it returns, so a device the user ends up declining, or a wave cut short by Ctrl+C, never
///     stays lit on the desk.
/// </remarks>
public static class DeviceIdentificationWave {

    private const int LedCount = 6;
    private const int Passes   = 2;
    private const int StepMs   = 40;

    #region Statics members declarations

    /// <summary>
    ///     Lights the six LEDs one after the other in white, twice, then turns them off. Throws
    ///     <see cref="DeviceCommandFailedException" /> when the device refuses a frame or cannot be reached,
    ///     and <see cref="OperationCanceledException" /> when interrupted at one of its waits.
    /// </summary>
    public static async Task PlayAsync(ILuxaforDevice device, IDelay delay, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(delay);

        try {
            for (int pass = 0; pass < Passes; pass++) {
                for (byte led = 1; led <= LedCount; led++) {
                    device.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(led), BrightColor.White));
                    await delay.WaitAsync(StepMs, cancellationToken).ConfigureAwait(false);
                }
            }
        } catch {
            // The caller is owed the reason the wave stopped, not the outcome of the clean-up: a LED left
            // lit is a small nuisance next to the cancellation or the device failure being reported.
            TurnOffBestEffort(device);

            throw;
        }

        // On a wave that ran to its end a refusal does matter: the device is about to be offered to the
        // user as working, and a device that cannot even switch off is not.
        device.TurnOffOrThrow();
    }

    private static void TurnOffBestEffort(ILuxaforDevice device) {
        try {
            device.TurnOff();
        } catch (Exception) {
            // Reported, if at all, through the exception already on its way to the caller.
        }
    }

    #endregion

}
