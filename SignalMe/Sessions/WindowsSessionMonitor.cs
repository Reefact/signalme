#region Usings declarations

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

using Microsoft.Win32;

#endregion

namespace SignalMe.Sessions;

/// <summary>
///     Watches the lock state of the Windows session signalme runs in.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="SystemEvents.SessionSwitch" /> registers for the calling session only, which is what
///         the spec asks for: another user locking their own session on the same machine is never seen.
///         Only the lock and unlock reasons are listened to; a remote connect or a logoff says nothing
///         about whether the user is at the keyboard.
///     </para>
///     <para>
///         The event is raised on the library's own ".NET System Events" thread, which it spawns and pumps
///         by itself since .NET 6 whatever the apartment of the caller, so SignalMe needs no message loop
///         and <see cref="Start" /> may run from any thread. Main must never be <c>[STAThread]</c> all the
///         same: the older SystemEvents shared an STA caller's thread and counted on it to pump messages,
///         which a console application never does, so the events simply never came. <see cref="Start" />
///         refuses an STA thread for that reason: the failure is loud and at start-up, instead of an
///         "away" that never shows up should the library ever share the thread again.
///     </para>
///     <para>
///         The handler must never block, never touch the device, and above all never throw:
///         <see cref="SystemEvents" /> swallows a handler's exception and then drops that handler from its
///         list, so one throw would silently end session monitoring for the rest of the process. It only
///         tells the coordinator, which queues the change and returns.
///     </para>
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowsSessionMonitor : ISessionMonitor {

    private const int  WTSSessionInfoEx        = 25;
    private const uint WTS_CURRENT_SESSION     = 0xFFFFFFFF;
    private const int  WTS_SESSIONSTATE_LOCK   = 0;
    private const int  WTS_SESSIONSTATE_UNLOCK = 1;

    #region Statics members declarations

    private static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

    /// <summary>
    ///     Asks WTS whether the current session is locked. Anything short of a clear "locked" answer, a
    ///     failed call included, reads as active: on a machine where the query is unavailable SignalMe
    ///     must keep showing the status rather than go away for good.
    /// </summary>
    private static SessionState QueryLockState() {
        IntPtr buffer = IntPtr.Zero;
        try {
            if (!WTSQuerySessionInformationW(WTS_CURRENT_SERVER_HANDLE, WTS_CURRENT_SESSION, WTSSessionInfoEx, out buffer, out uint bytes)) { return SessionState.Active; }
            if (bytes < (uint)Marshal.SizeOf<WTSINFOEX>()) { return SessionState.Active; }

            WTSINFOEX info = Marshal.PtrToStructure<WTSINFOEX>(buffer);
            if (info.Level != 1) { return SessionState.Active; }

            // .NET 10 does not run on Windows 7, so the historical inversion of these two flags is of no
            // concern here.
            return info.Data.SessionFlags switch {
                WTS_SESSIONSTATE_LOCK   => SessionState.Locked,
                WTS_SESSIONSTATE_UNLOCK => SessionState.Active,
                _                       => SessionState.Active
            };
        } catch (Exception) {
            return SessionState.Active;
        } finally {
            if (buffer != IntPtr.Zero) { WTSFreeMemory(buffer); }
        }
    }

    [DllImport("wtsapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool WTSQuerySessionInformationW(IntPtr server, uint sessionId, int infoClass, out IntPtr buffer, out uint bytesReturned);

    [DllImport("wtsapi32.dll")]
    private static extern void WTSFreeMemory(IntPtr memory);

    #endregion

    #region Fields declarations

    private readonly object       _lock = new();
    private volatile SessionState _current;
    private volatile bool         _disposed;

    #endregion

    #region Constructors declarations

    public WindowsSessionMonitor() {
        _current = QueryLockState();
    }

    #endregion

    /// <inheritdoc />
    public SessionState Current => _current;

    /// <inheritdoc />
    public event EventHandler<SessionState>? StateChanged;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Called from an STA thread; see the class remarks.</exception>
    /// <exception cref="ExternalException">The library could not create the hidden window it receives the notifications through.</exception>
    public void Start() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) { throw new InvalidOperationException("Windows session events cannot be watched from an STA thread: the entry point must not be marked [STAThread]."); }

        // Subscribed before the state is read, so that a lock happening right now is either seen by the
        // query or delivered as an event. The other order would leave a hole between the two.
        SystemEvents.SessionSwitch += OnSessionSwitch;

        // The query runs under the lock as well: an event arriving in between would otherwise be
        // overwritten by the older answer of the query.
        lock (_lock) { Publish(QueryLockState()); }
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) { return; }

        // Flagged before unsubscribing: SystemEvents snapshots its handler list outside its lock, so one
        // call can still arrive after the handler was removed, and that call must find the flag set.
        _disposed = true;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e) {
        if (_disposed) { return; }

        try {
            SessionState? state = e.Reason switch {
                SessionSwitchReason.SessionLock   => SessionState.Locked,
                SessionSwitchReason.SessionUnlock => SessionState.Active,
                _                                 => null
            };
            if (state is null) { return; }

            lock (_lock) { Publish(state.Value); }
        } catch (Exception) {
            // Nothing may get out: SystemEvents would swallow it and drop this handler, ending session
            // monitoring silently for the rest of the process.
        }
    }

    /// <summary>
    ///     Records a state and tells the subscribers, only when it is actually a change. Runs under the
    ///     lock, so that the start-up query and an event arriving at the same moment cannot cross and leave
    ///     <see cref="Current" /> saying one thing and the last event another. The subscriber is the
    ///     coordinator's queue, which never blocks, so the lock is held for a moment only.
    /// </summary>
    private void Publish(SessionState state) {
        if (_current == state) { return; }

        _current = state;
        StateChanged?.Invoke(this, state);
    }

    #region Nested types declarations

    /// <summary>
    ///     WTSINFOEX_LEVEL1_W, laid out exactly: the wide strings are inlined, so every field after them
    ///     sits at the offset the native struct puts it at.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WTSINFOEX_LEVEL1 {

        public uint SessionId;
        public int  SessionState;
        public int  SessionFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)] public string WinStationName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 21)] public string UserName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 18)] public string DomainName;
        public long LogonTime, ConnectTime, DisconnectTime, LastInputTime, CurrentTime;
        public uint IncomingBytes, OutgoingBytes, IncomingFrames, OutgoingFrames, IncomingCompressedBytes, OutgoingCompressedBytes;

    }

    /// <summary>
    ///     WTSINFOEXW. The Data union holds 64-bit integers and is therefore 8-byte aligned, which puts
    ///     SessionFlags at offset 16, not 12: declaring the union as its only member keeps that padding.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct WTSINFOEX {

        public uint             Level;
        public WTSINFOEX_LEVEL1 Data;

    }

    #endregion

}
