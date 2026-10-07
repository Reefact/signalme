#region Usings declarations

using System;
using System.Threading;

#endregion

namespace SignalMe.Infrastructure;

public static class ConsoleCancellation {

    #region Statics members declarations

    /// <summary>
    ///     Returns a source cancelled the first time the user presses Ctrl+C.
    /// </summary>
    /// <remarks>
    ///     Setting <c>Cancel = true</c> stops the runtime from killing the process on the spot, which is what
    ///     lets the selector or the runtime end cleanly: the mode and any animation are stopped, the device
    ///     is turned off and released, and only then does signalme exit (spec §33).
    /// </remarks>
    public static CancellationTokenSource OnCtrlC() {
        CancellationTokenSource cancellation = new();

        Console.CancelKeyPress += (_, eventArgs) => {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        return cancellation;
    }

    #endregion

}
