#region Usings declarations

using System;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class ReadyPattern {

    #region Fields declarations

    private readonly LuxaforDevice        _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public ReadyPattern(LuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice);
    }

    #endregion

    public void Play() {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        if (currentUserStatus == UserStatus.DoNotDisturb) {
            Thread.Sleep(1000);
            _luxaforDevice.SetColor(PredefinedColor.Busy);
            currentUserStatus = UserStatus.Busy;
        }

        if (currentUserStatus == UserStatus.Busy) {
            Thread.Sleep(2000);
        }

        for (int i = 0; i < 15; i++) {
            _luxaforDevice.TurnOff();
            Thread.Sleep(10);
            _luxaforDevice.SetColor(PredefinedColor.Available);
            Thread.Sleep(50);
        }

        _userStatusController.Display(UserStatus.Available);
    }

}