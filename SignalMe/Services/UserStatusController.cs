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

    public UserStatusController(LuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        Device = luxaforDevice;
    }

    #endregion

    public LuxaforDevice Device { get; }

    public BrightColor GetUserStatusColor(UserStatus? userStatus) {
        if (userStatus == null) { return BrightColor.Black; }

        if (!Enum.IsDefined(typeof(UserStatus), userStatus)) { throw new InvalidEnumArgumentException(nameof(userStatus), (int)userStatus, typeof(UserStatus)); }

        return _colorByUserStatus[userStatus.Value];
    }

    public void Display(UserStatus? status) {
        if (status == null) {
            Device.TurnOff();

            return;
        }

        if (!Enum.IsDefined(typeof(UserStatus), status)) { throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(UserStatus)); }

        if (!_colorByUserStatus.TryGetValue(status.Value, out BrightColor? statusColor)) { throw new Exception(); }

        Device.SetColor(statusColor);
        UserCurrentStatus.Set(status);
    }

    public UserStatus? GetUserCurrentStatus() {
        return UserCurrentStatus.Get();
    }

}