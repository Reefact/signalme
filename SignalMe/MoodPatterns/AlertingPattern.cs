#region Usings declarations

using System;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

/// <summary>
///     Flashes red twenty times, fast, then goes back to the durable status.
/// </summary>
public sealed class AlertingPattern {

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;
    private readonly IDelay               _delay;

    #endregion

    #region Constructors declarations

    public AlertingPattern(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null, IDelay? delay = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice, userCurrentStatus);
        _delay                = delay ?? RealDelay.Instance;
    }

    #endregion

    public Task PlayAsync(CancellationToken cancellationToken) {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        return _userStatusController.PlayAndRestoreAsync(currentUserStatus, async () => {
            for (int i = 0; i < 20; i++) {
                _luxaforDevice.SetColorOrThrow(BrightColor.Red);
                await _delay.WaitAsync(50, cancellationToken).ConfigureAwait(false);
                _luxaforDevice.TurnOffOrThrow();
                await _delay.WaitAsync(50, cancellationToken).ConfigureAwait(false);
            }
        });
    }

}
