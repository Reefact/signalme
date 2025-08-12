#region Usings declarations

using System;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class DesperatePattern {

    #region Fields declarations

    private readonly LuxaforDevice        _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public DesperatePattern(LuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice);
    }

    #endregion

    public void Play() {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        Thread.Sleep(1000);
        _luxaforDevice.TurnOff();
        Thread.Sleep(300);

        _luxaforDevice.SetColor(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOff();
        Thread.Sleep(100);
        _luxaforDevice.SetColor(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOff();
        Thread.Sleep(100);
        _luxaforDevice.SetColor(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOff();
        Thread.Sleep(300);

        _luxaforDevice.SetColor(BrightColor.White);
        Thread.Sleep(500);
        _luxaforDevice.TurnOff();
        Thread.Sleep(100);
        _luxaforDevice.SetColor(BrightColor.White);
        Thread.Sleep(500);
        _luxaforDevice.TurnOff();
        Thread.Sleep(100);
        _luxaforDevice.SetColor(BrightColor.White);
        Thread.Sleep(500);
        _luxaforDevice.TurnOff();
        Thread.Sleep(300);

        _luxaforDevice.SetColor(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOff();
        Thread.Sleep(100);
        _luxaforDevice.SetColor(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOff();
        Thread.Sleep(100);
        _luxaforDevice.SetColor(BrightColor.White);
        Thread.Sleep(100);
        _luxaforDevice.TurnOff();

        Thread.Sleep(1000);

        _userStatusController.Display(currentUserStatus);
    }

}