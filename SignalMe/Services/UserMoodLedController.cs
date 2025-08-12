#region Usings declarations

using System;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.MoodPatterns;

#endregion

namespace SignalMe.Services;

public sealed class UserMoodLedController {

    #region Fields declarations

    private readonly LuxaforDevice _luxaforDevice;

    #endregion

    #region Constructors declarations

    public UserMoodLedController(LuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice = luxaforDevice;
    }

    #endregion

    public void Display(UserMood userMood) {
        switch (userMood) {
            case UserMood.Happy:
                new HappyPattern(_luxaforDevice).Play();

                break;
            case UserMood.Desperate:
                new DesperatePattern(_luxaforDevice).Play();

                break;
            case UserMood.Warning:
                new WarningPattern(_luxaforDevice).Play();

                break;
            case UserMood.Alerting:
                new AlertingPattern(_luxaforDevice).Play();

                break;
            case UserMood.Ready:
                new ReadyPattern(_luxaforDevice).Play();

                break;
            case UserMood.Bored:
                new BoredPattern(_luxaforDevice).Play();

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}