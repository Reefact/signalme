#region Usings declarations

using System;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class AlertingPattern {

    #region Fields declarations

    private readonly LuxaforDevice        _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public AlertingPattern(LuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice);
    }

    #endregion

    public void Play() {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        for (int i = 0; i < 20; i++) {
            _luxaforDevice.SetColor(BrightColor.Red);
            Thread.Sleep(50);
            _luxaforDevice.TurnOff();
            Thread.Sleep(50);
        }

        _userStatusController.Display(currentUserStatus);
    }

}