#region Usings declarations

using System;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     Thrown when a Luxafor device refuses a lighting command, that is when it answers false instead of
///     acknowledging the write.
/// </summary>
/// <remarks>
///     The device commands report failure through a return value rather than an exception, so nothing
///     stops the caller from carrying on as if the LEDs had changed. This exception turns that silent
///     false into an error the CLI can report and exit on.
/// </remarks>
public sealed class DeviceCommandFailedException : Exception {

    #region Constructors declarations

    public DeviceCommandFailedException(string message) : base(message) { }

    #endregion

}
