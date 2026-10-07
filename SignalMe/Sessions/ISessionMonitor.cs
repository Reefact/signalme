#region Usings declarations

using System;

#endregion

namespace SignalMe.Sessions;

/// <summary>
///     Watches the Windows session signalme runs in.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Current" /> is readable at any time. <see cref="StateChanged" /> may be raised from any
///         thread, only on an actual change, with the new state; <see cref="Current" /> is updated before
///         the event is raised, so a handler reading it sees the state it was told about.
///     </para>
///     <para>
///         <see cref="Start" /> subscribes to the OS first and then re-reads the state, raising
///         <see cref="StateChanged" /> if it differs from what <see cref="Current" /> said so far: that
///         closes the gap between construction and subscription, during which a lock would otherwise go
///         unnoticed. <see cref="IDisposable.Dispose" /> unsubscribes and is idempotent; a late OS event
///         arriving after it is dropped.
///     </para>
/// </remarks>
public interface ISessionMonitor : IDisposable {

    SessionState Current { get; }

    event EventHandler<SessionState>? StateChanged;

    void Start();

}
