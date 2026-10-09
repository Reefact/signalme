#region Usings declarations

using System;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Devices;
using SignalMe.Sessions;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     Everything the command line needs from the outside world, each with its real implementation as the
///     default.
/// </summary>
/// <remarks>
///     The command app attaches it to the command context, which is how the tests run the real command
///     line, option parsing and exit codes included, over a console they script and devices they fake. A
///     bag rather than a container: the command has a handful of dependencies and Spectre's own resolver
///     builds the command itself.
/// </remarks>
public sealed class SignalMeServices {

    #region Statics members declarations

    /// <summary>
    ///     Session events exist on Windows only; elsewhere the session is assumed active and never changes,
    ///     the device discovery being the real gate there. The platform check is also what lets the
    ///     Windows-only monitor be named here at all: the analyzer refuses it outside such a guard.
    /// </summary>
    private static ISessionMonitor CreateDefaultSessionMonitor() {
        return OperatingSystem.IsWindows() ? new WindowsSessionMonitor() : new StaticSessionMonitor(SessionState.Active);
    }

    #endregion

    public IConsole                                       Console               { get; init; } = SystemConsole.Instance;
    public IDelay                                         Delay                 { get; init; } = RealDelay.Instance;
    public ILuxaforDeviceDiscovery                        Discovery             { get; init; } = LuxaforDeviceDiscovery.Instance;
    public UserCurrentStatus                              Store                 { get; init; } = UserCurrentStatus.Default;
    public Func<ISessionMonitor>                          SessionMonitorFactory { get; init; } = CreateDefaultSessionMonitor;
    public Func<ILuxaforDevice, IDeviceConnectionMonitor> DeviceMonitorFactory  { get; init; } = device => new LuxaforDeviceConnectionMonitor(device);

}
