#region Usings declarations

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SignalMe.Modes;

/// <summary>
///     A source of intents: it collects whatever determines the status (the console, one day a presence
///     service) and asks the coordinator through its context. It knows neither the device nor the colours.
/// </summary>
public interface ISignalMeMode {

    /// <summary>
    ///     Runs until the mode has nothing more to do (for instance the console input closed) or the token is
    ///     cancelled. On cancellation it may return normally or throw <see cref="System.OperationCanceledException" />:
    ///     the runtime accepts both. Any other exception coming out of
    ///     <see cref="ISignalMeContext.ExecuteAsync" /> is left to propagate, so the runtime can classify it;
    ///     the mode does not print it.
    /// </summary>
    Task RunAsync(ISignalMeContext context, CancellationToken cancellationToken);

}
