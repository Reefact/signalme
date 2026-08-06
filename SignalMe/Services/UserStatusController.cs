#region Usings declarations

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;

#endregion

namespace SignalMe.Services;

public sealed class UserStatusController {

    #region Statics members declarations

    private static readonly Dictionary<UserStatus, BrightColor> _colorByUserStatus;

    static UserStatusController() {
        _colorByUserStatus = new Dictionary<UserStatus, BrightColor> {
            { UserStatus.Available, PredefinedColor.Available },
            { UserStatus.Away, PredefinedColor.Away },
            { UserStatus.Busy, PredefinedColor.Busy },
            { UserStatus.DoNotDisturb, PredefinedColor.DoNotDisturb }
        };
    }

    #endregion

    #region Constructors declarations

    public UserStatusController(ILuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        Device = luxaforDevice;
    }

    #endregion

    public ILuxaforDevice Device { get; }

    public BrightColor GetUserStatusColor(UserStatus? userStatus) {
        if (userStatus == null) { return BrightColor.Black; }

        if (!Enum.IsDefined(typeof(UserStatus), userStatus)) { throw new InvalidEnumArgumentException(nameof(userStatus), (int)userStatus, typeof(UserStatus)); }

        return _colorByUserStatus[userStatus.Value];
    }

    /// <summary>
    ///     Displays <paramref name="status" /> on the device and remembers it, or turns the device off when
    ///     no status is given.
    /// </summary>
    /// <exception cref="DeviceCommandFailedException">The device refused the command.</exception>
    public void Display(UserStatus? status) {
        if (status == null) {
            if (!Device.TurnOff()) { throw new DeviceCommandFailedException("The Luxafor device refused to turn its LEDs off."); }

            return;
        }

        if (!Enum.IsDefined(typeof(UserStatus), status)) { throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(UserStatus)); }

        if (!_colorByUserStatus.TryGetValue(status.Value, out BrightColor? statusColor)) { throw new InvalidOperationException($"No color is configured for the '{status.Value}' status."); }

        // The status is remembered only once the device has acknowledged the write, otherwise the
        // remembered status would describe a color the LEDs never displayed.
        if (!Device.SetColor(statusColor)) { throw new DeviceCommandFailedException($"The Luxafor device refused to display the '{status.Value}' status."); }

        UserCurrentStatus.Set(status);
    }

    /// <summary>
    ///     Restores <paramref name="status" /> at the end of an animation, reporting a failure instead of
    ///     throwing.
    /// </summary>
    /// <remarks>
    ///     Called from a finally block: throwing here would replace whatever error interrupted the
    ///     animation with a less useful one. The failure is written to the error output instead.
    /// </remarks>
    public void TryRestore(UserStatus? status) {
        try {
            Display(status);
        } catch (Exception exception) {
            Console.Error.WriteLine($"The animation is over but the previous status could not be restored: {exception.Message}");
        }
    }

    public UserStatus? GetUserCurrentStatus() {
        return UserCurrentStatus.Get();
    }

}
