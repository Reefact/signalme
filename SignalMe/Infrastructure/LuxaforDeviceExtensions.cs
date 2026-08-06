#region Usings declarations

using System;

using Reefact.LuxaforLightingDeviceController;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     Sends commands to a Luxafor device, turning a refusal into a
///     <see cref="DeviceCommandFailedException" />.
/// </summary>
/// <remarks>
///     The device reports a refused command by returning false rather than by throwing, so ignoring the
///     return value lets signalme carry on — and report success — while the LEDs never changed. These are
///     the only device calls signalme makes: one policy, one exception, one message shape.
/// </remarks>
public static class LuxaforDeviceExtensions {

    #region Statics members declarations

    /// <summary>Turns every LED on in <paramref name="color" />.</summary>
    /// <param name="device">The device to drive.</param>
    /// <param name="color">The color to display.</param>
    /// <param name="operation">
    ///     What the caller was trying to do, for the error message. Defaults to describing the color.
    /// </param>
    /// <exception cref="DeviceCommandFailedException">The device refused the command.</exception>
    public static void SetColorOrThrow(this ILuxaforDevice device, BrightColor color, string? operation = null) {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(color);

        if (!device.SetColor(color)) { throw Refused(operation ?? $"display the color {color}"); }
    }

    /// <summary>Turns every LED off.</summary>
    /// <exception cref="DeviceCommandFailedException">The device refused the command.</exception>
    public static void TurnOffOrThrow(this ILuxaforDevice device, string? operation = null) {
        ArgumentNullException.ThrowIfNull(device);

        if (!device.TurnOff()) { throw Refused(operation ?? "turn its LEDs off"); }
    }

    /// <summary>Sends a lighting command, typically one targeting a single LED.</summary>
    /// <exception cref="DeviceCommandFailedException">The device refused the command.</exception>
    public static void SendOrThrow(this ILuxaforDevice device, LightingCommand command) {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(command);

        // LightingCommand.ToString() reads as "Set LED n° 3 color to #00FF00", which names the failing
        // operation better than anything this method could reconstruct.
        if (!device.Send(command)) { throw Refused($"run the command \"{command}\""); }
    }

    private static DeviceCommandFailedException Refused(string operation) {
        return new DeviceCommandFailedException($"The Luxafor device refused to {operation}.");
    }

    #endregion

}
