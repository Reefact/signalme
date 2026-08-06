#region Usings declarations

using System;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.MoodPatterns;

#endregion

namespace SignalMe.Services;

public sealed class UserMoodLedController {

    #region Fields declarations

    private readonly ILuxaforDevice    _luxaforDevice;
    private readonly UserCurrentStatus _userCurrentStatus;

    #endregion

    #region Constructors declarations

    public UserMoodLedController(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice     = luxaforDevice;
        _userCurrentStatus = userCurrentStatus ?? UserCurrentStatus.Default;
    }

    #endregion

    public void Display(UserMood userMood) {
        switch (userMood) {
            case UserMood.Happy:
                new HappyPattern(_luxaforDevice, _userCurrentStatus).Play();

                break;
            case UserMood.Desperate:
                new DesperatePattern(_luxaforDevice, _userCurrentStatus).Play();

                break;
            case UserMood.Warning:
                new WarningPattern(_luxaforDevice, _userCurrentStatus).Play();

                break;
            case UserMood.Alerting:
                new AlertingPattern(_luxaforDevice, _userCurrentStatus).Play();

                break;
            case UserMood.Ready:
                new ReadyPattern(_luxaforDevice, _userCurrentStatus).Play();

                break;
            case UserMood.Bored:
                new BoredPattern(_luxaforDevice, _userCurrentStatus).Play();

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}