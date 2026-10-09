#region Usings declarations

using System;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     The one shape an error takes on the error output, so that the coordinator's loop, the runtime and
///     the command line cannot drift apart in how they describe the same failure.
/// </summary>
public static class ErrorReporting {

    #region Statics members declarations

    /// <summary>
    ///     A device failure already reads as a sentence and is printed as it is; anything else is prefixed
    ///     with the tool's name and the exception type, the way a CLI reports what it did not expect.
    /// </summary>
    public static string Describe(Exception exception) {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is DeviceCommandFailedException or DeviceDisconnectedException ? exception.Message : $"signalme: {exception.GetType().Name}: {exception.Message}";
    }

    #endregion

}
