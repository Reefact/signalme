#region Usings declarations

using System;

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     Sends commands to a Luxafor device, turning a refusal or a failed call into a
///     <see cref="DeviceCommandFailedException" />.
/// </summary>
/// <remarks>
///     The device reports a refused command by returning false rather than by throwing, so ignoring the
///     return value lets signalme carry on — and report success — while the LEDs never changed. A device
///     that is unplugged mid-run throws from the HID layer instead; that is wrapped too, so one exception
///     type covers "the device did not do it" whatever the cause. Every device call signalme makes goes
///     through here, the best-effort turn-off at shutdown included: that one reports instead of throwing,
///     since by then there is nothing left to do about a failure.
/// </remarks>
public static class LuxaforDeviceExtensions {

    #region Statics members declarations

    /// <summary>Turns every LED on in <paramref name="color" />.</summary>
    /// <param name="device">The device to drive.</param>
    /// <param name="color">The color to display.</param>
    /// <param name="operation">
    ///     What the caller was trying to do, for the error message. Defaults to describing the color.
    /// </param>
    /// <exception cref="DeviceCommandFailedException">The device refused the command or could not be reached.</exception>
    public static void SetColorOrThrow(this ILuxaforDevice device, BrightColor color, string? operation = null) {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(color);

        if (!Execute(() => device.SetColor(color))) { throw Refused(operation ?? $"display the color {color}"); }
    }

    /// <summary>Turns every LED off.</summary>
    /// <exception cref="DeviceCommandFailedException">The device refused the command or could not be reached.</exception>
    public static void TurnOffOrThrow(this ILuxaforDevice device, string? operation = null) {
        ArgumentNullException.ThrowIfNull(device);

        if (!Execute(device.TurnOff)) { throw Refused(operation ?? "turn its LEDs off"); }
    }

    /// <summary>Sends a lighting command, typically one targeting a single LED.</summary>
    /// <exception cref="DeviceCommandFailedException">The device refused the command or could not be reached.</exception>
    public static void SendOrThrow(this ILuxaforDevice device, LightingCommand command) {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(command);

        // LightingCommand.ToString() reads as "Set LED n° 3 color to #00FF00", which names the failing
        // operation better than anything this method could reconstruct.
        if (!Execute(() => device.Send(command))) { throw Refused($"run the command \"{command}\""); }
    }

    /// <summary>
    ///     Turns every LED off, best effort: a refusal or an exception is reported on the error output of
    ///     <paramref name="console" /> and never thrown. For the exits where nothing can be done about a
    ///     failure any more, the shutdown first of all.
    /// </summary>
    public static void TurnOffQuietly(this ILuxaforDevice device, IConsole console) {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(console);

        try {
            if (!device.TurnOff()) { console.WriteError("The Luxafor device refused to turn its LEDs off."); }
        } catch (Exception exception) {
            console.WriteError($"The Luxafor device could not be turned off: {exception.GetType().Name}: {exception.Message}");
        }
    }

    /// <summary>
    ///     Runs one device call. A cancellation is not a device failure and goes through untouched; anything
    ///     else the call throws is wrapped, with the original kept as the inner exception for diagnostics.
    /// </summary>
    private static bool Execute(Func<bool> command) {
        try {
            return command();
        } catch (Exception exception) when (exception is not OperationCanceledException) {
            throw new DeviceCommandFailedException($"The Luxafor device could not be reached: {exception.GetType().Name}: {exception.Message}", exception);
        }
    }

    private static DeviceCommandFailedException Refused(string operation) {
        return new DeviceCommandFailedException($"The Luxafor device refused to {operation}.");
    }

    #endregion

}
