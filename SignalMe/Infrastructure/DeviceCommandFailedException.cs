#region Usings declarations

using System;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     Thrown when a Luxafor device refuses a lighting command, that is when it answers false instead of
///     acknowledging the write, or when the call to the device itself throws.
/// </summary>
/// <remarks>
///     The device commands report failure through a return value rather than an exception, so nothing
///     stops the caller from carrying on as if the LEDs had changed. This exception turns that silent
///     false into an error the CLI can report and exit on. A HID exception is wrapped into it too, so that
///     this type means exactly "a device call failed" and the runtime can map outcomes on the type alone.
/// </remarks>
public sealed class DeviceCommandFailedException : Exception {

    #region Constructors declarations

    public DeviceCommandFailedException(string message) : base(message) { }

    public DeviceCommandFailedException(string message, Exception innerException) : base(message, innerException) { }

    #endregion

}
