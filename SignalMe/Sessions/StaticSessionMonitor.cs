#region Usings declarations

using System;

#endregion

namespace SignalMe.Sessions;

/// <summary>
///     A session that is always in one state and never changes.
/// </summary>
/// <remarks>
///     The fallback on systems without session events: the session is assumed active there and the device
///     discovery is the real gate, which keeps the tool smoke-testable where no Luxafor can be reached.
/// </remarks>
public sealed class StaticSessionMonitor : ISessionMonitor {

    #region Constructors declarations

    public StaticSessionMonitor(SessionState state) {
        Current = state;
    }

    #endregion

    /// <inheritdoc />
    public SessionState Current { get; }

    /// <inheritdoc />
    /// <remarks>Never raised: the state cannot change. Explicit accessors keep the compiler from flagging an unused event.</remarks>
    public event EventHandler<SessionState>? StateChanged {
        add { }
        remove { }
    }

    /// <inheritdoc />
    public void Start() { }

    /// <inheritdoc />
    public void Dispose() { }

}
