#region Usings declarations

using System;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class DesperatePattern {

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public DesperatePattern(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice, userCurrentStatus);
    }

    #endregion

    public void Play() {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        _userStatusController.PlayAndRestore(currentUserStatus, () => {
            PlaySequence();
                });
    }

    private void PlaySequence() {
        Thread.Sleep(1000);
        _luxaforDevice.TurnOffOrThrow();
        Thread.Sleep(300);

        _luxaforDevice.SetColorOrThrow(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOffOrThrow();
        Thread.Sleep(100);
        _luxaforDevice.SetColorOrThrow(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOffOrThrow();
        Thread.Sleep(100);
        _luxaforDevice.SetColorOrThrow(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOffOrThrow();
        Thread.Sleep(300);

        _luxaforDevice.SetColorOrThrow(BrightColor.White);
        Thread.Sleep(500);
        _luxaforDevice.TurnOffOrThrow();
        Thread.Sleep(100);
        _luxaforDevice.SetColorOrThrow(BrightColor.White);
        Thread.Sleep(500);
        _luxaforDevice.TurnOffOrThrow();
        Thread.Sleep(100);
        _luxaforDevice.SetColorOrThrow(BrightColor.White);
        Thread.Sleep(500);
        _luxaforDevice.TurnOffOrThrow();
        Thread.Sleep(300);

        _luxaforDevice.SetColorOrThrow(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOffOrThrow();
        Thread.Sleep(100);
        _luxaforDevice.SetColorOrThrow(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOffOrThrow();
        Thread.Sleep(100);
        _luxaforDevice.SetColorOrThrow(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOffOrThrow();

        Thread.Sleep(1000);
    }

}