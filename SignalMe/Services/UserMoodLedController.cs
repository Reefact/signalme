#region Usings declarations

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.MoodPatterns;

#endregion

namespace SignalMe.Services;

public sealed class UserMoodLedController {

    #region Fields declarations

    private readonly ILuxaforDevice     _luxaforDevice;
    private readonly UserCurrentStatus? _userCurrentStatus;
    private readonly IDelay?            _delay;

    #endregion

    #region Constructors declarations

    public UserMoodLedController(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null, IDelay? delay = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice     = luxaforDevice;
        _userCurrentStatus = userCurrentStatus;
        _delay             = delay;
    }

    #endregion

    public Task DisplayAsync(UserMood userMood, CancellationToken cancellationToken) {
        return userMood switch {
            UserMood.Happy     => new HappyPattern(_luxaforDevice, _userCurrentStatus, _delay).PlayAsync(cancellationToken),
            UserMood.Desperate => new DesperatePattern(_luxaforDevice, _userCurrentStatus, _delay).PlayAsync(cancellationToken),
            UserMood.Warning   => new WarningPattern(_luxaforDevice, _userCurrentStatus, _delay).PlayAsync(cancellationToken),
            UserMood.Alerting  => new AlertingPattern(_luxaforDevice, _userCurrentStatus, _delay).PlayAsync(cancellationToken),
            UserMood.Ready     => new ReadyPattern(_luxaforDevice, _userCurrentStatus, _delay).PlayAsync(cancellationToken),
            UserMood.Bored     => new BoredPattern(_luxaforDevice, _userCurrentStatus, _delay).PlayAsync(cancellationToken),
            _                  => throw new InvalidEnumArgumentException(nameof(userMood), (int)userMood, typeof(UserMood))
        };
    }

}
