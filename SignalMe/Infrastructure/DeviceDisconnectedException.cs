#region Usings declarations

using System;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     Thrown when the device signalme drives is found unplugged during the run.
/// </summary>
/// <remarks>
///     Not a <see cref="DeviceCommandFailedException" />: no command failed, the device was seen missing
///     before anything was asked of it. The runtime maps it to the same device error, and knows from this
///     type alone not to write to the device again on its way out.
/// </remarks>
public sealed class DeviceDisconnectedException : Exception {

    #region Constructors declarations

    public DeviceDisconnectedException() : base("Luxafor device disconnected.") { }

    public DeviceDisconnectedException(string message) : base(message) { }

    public DeviceDisconnectedException(string message, Exception innerException) : base(message, innerException) { }

    #endregion

}
