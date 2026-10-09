#region Usings declarations

using System;

#endregion

namespace SignalMe.Devices;

/// <summary>
///     Watches whether the device signalme drives is still plugged in.
/// </summary>
/// <remarks>
///     <para>
///         Without it, an unplugged device goes unnoticed until the next write: SignalMe keeps saying the
///         status is shown while the device is dark, and only stops when a command, a lock or a signal
///         frame fails. Rebinding the device would not help either, since the handle opened at start-up
///         stays dead once the device is gone, whatever port it comes back on.
///     </para>
///     <para>
///         <see cref="Disconnected" /> is raised at most once, from any thread, and never touches the
///         device: it only tells whoever subscribed. <see cref="IDisposable.Dispose" /> stops the watch and
///         is idempotent; a check already in flight when it is called raises nothing.
///     </para>
/// </remarks>
public interface IDeviceConnectionMonitor : IDisposable {

    event EventHandler? Disconnected;

    void Start();

}
