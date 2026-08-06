#region Usings declarations

using System;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class AlertingPattern {

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public AlertingPattern(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice, userCurrentStatus);
    }

    #endregion

    public void Play() {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        _userStatusController.PlayAndRestore(currentUserStatus, () => {
            for (int i = 0; i < 20; i++) {
                _luxaforDevice.SetColorOrThrow(BrightColor.Red);
                Thread.Sleep(50);
                _luxaforDevice.TurnOffOrThrow();
                Thread.Sleep(50);
            }
                });
    }

}