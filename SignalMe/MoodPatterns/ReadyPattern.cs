#region Usings declarations

using System;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class ReadyPattern {

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public ReadyPattern(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice, userCurrentStatus);
    }

    #endregion

    public void Play() {
        UserStatus? previousStatus = _userStatusController.GetUserCurrentStatus();

        // "ready" is the one pattern that deliberately changes the durable status: it announces that the
        // user has become available. Until it gets there, the status to fall back on is the previous one.
        _userStatusController.PlayAndRestore(previousStatus, () => {
            UserStatus? displayedStatus = previousStatus;

            if (displayedStatus == UserStatus.DoNotDisturb) {
                Thread.Sleep(1000);
                _luxaforDevice.SetColorOrThrow(PredefinedColor.Busy);
                displayedStatus = UserStatus.Busy;
            }

            if (displayedStatus == UserStatus.Busy) {
                Thread.Sleep(2000);
            }

            for (int i = 0; i < 15; i++) {
                _luxaforDevice.TurnOffOrThrow();
                Thread.Sleep(10);
                _luxaforDevice.SetColorOrThrow(PredefinedColor.Available);
                Thread.Sleep(50);
            }

            return UserStatus.Available;
        });
    }

}