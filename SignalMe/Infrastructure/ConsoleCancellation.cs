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
    ///     gives an animation the chance to put the durable status back before signalme exits.
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
