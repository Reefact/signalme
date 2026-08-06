#region Usings declarations

using System;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class ReadyPattern {

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public ReadyPattern(ILuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice);
    }

    #endregion

    public void Play() {
        UserStatus? previousStatus = _userStatusController.GetUserCurrentStatus();
        // "ready" is the one pattern that deliberately changes the durable status. Until it gets there,
        // the status to fall back on is the previous one.
        UserStatus? statusToRestore = previousStatus;

        try {
            UserStatus? displayedStatus = previousStatus;

            if (displayedStatus == UserStatus.DoNotDisturb) {
                Thread.Sleep(1000);
                _luxaforDevice.SetColor(PredefinedColor.Busy);
                displayedStatus = UserStatus.Busy;
            }

            if (displayedStatus == UserStatus.Busy) {
                Thread.Sleep(2000);
            }

            for (int i = 0; i < 15; i++) {
                _luxaforDevice.TurnOff();
                Thread.Sleep(10);
                _luxaforDevice.SetColor(PredefinedColor.Available);
                Thread.Sleep(50);
            }

            statusToRestore = UserStatus.Available;
        } finally {
            _userStatusController.TryRestore(statusToRestore);
        }
    }

}