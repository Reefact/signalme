#region Usings declarations

using System;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Converters;
using SignalMe.Infrastructure;

#endregion

namespace SignalMe.Services;

public sealed class SignalMeService {

    #region Fields declarations

    private readonly LuxaforDevice _luxaforDevice;

    #endregion

    #region Constructors declarations

    public SignalMeService(LuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice = luxaforDevice;
    }

    #endregion

    public void SetAs(string statusOrMood) {
        if (UserStatusConverter.TryConvert(statusOrMood, out UserStatus? userStatus)) {
            new UserStatusController(_luxaforDevice).Display(userStatus.Value);

            return;
        }

        if (UserMoodConverter.TryConvert(statusOrMood, out UserMood? userMood)) {
            new UserMoodLedController(_luxaforDevice).Display(userMood.Value);

            return;
        }

        throw new ArgumentException($"Unknown user status or mood : {statusOrMood}");
    }

    public void TurnOff() {
        _luxaforDevice.TurnOff();
        UserCurrentStatus.Set(null);
    }

}