#region Usings declarations

using System.Threading;
using System.Threading.Tasks;

using SignalMe.Runtime;

#endregion

namespace SignalMe.Modes;

/// <summary>
///     What a mode sees of SignalMe: its name, a snapshot of the state, and a way to send intents.
/// </summary>
public interface ISignalMeContext {

    string ModeName { get; }

    /// <summary>A thread-safe snapshot, refreshed after every message the coordinator handles.</summary>
    SignalMeState State { get; }

    /// <summary>
    ///     Completes once the coordinator has fully processed the intent; for a signal, once the animation
    ///     ended. Throws <see cref="System.OperationCanceledException" /> when the token is cancelled or the
    ///     coordinator has stopped, and rethrows the coordinator's failure (a
    ///     <see cref="Infrastructure.DeviceCommandFailedException" /> or anything else) when it faulted.
    /// </summary>
    Task<IntentOutcome> ExecuteAsync(SignalMeIntent intent, CancellationToken cancellationToken);

}
