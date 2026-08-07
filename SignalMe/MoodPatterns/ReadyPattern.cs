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
///     Announces that the user has become available: steps down from do-not-disturb through busy, blinks
///     green, and settles on the available status.
/// </summary>
/// <remarks>
///     The one pattern that deliberately changes the durable status instead of restoring it.
/// </remarks>
public sealed class ReadyPattern {

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;
    private readonly IDelay               _delay;

    #endregion

    #region Constructors declarations

    public ReadyPattern(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null, IDelay? delay = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice, userCurrentStatus);
        _delay                = delay ?? RealDelay.Instance;
    }

    #endregion

    public Task PlayAsync(CancellationToken cancellationToken) {
        UserStatus? previousStatus = _userStatusController.GetUserCurrentStatus();

        // Until the sequence reaches "available", the status to fall back on is the previous one.
        return _userStatusController.PlayAndRestoreAsync(previousStatus, async () => {
            UserStatus? displayedStatus = previousStatus;

            if (displayedStatus == UserStatus.DoNotDisturb) {
                await _delay.WaitAsync(1000, cancellationToken).ConfigureAwait(false);
                _luxaforDevice.SetColorOrThrow(PredefinedColor.Busy);
                displayedStatus = UserStatus.Busy;
            }

            if (displayedStatus == UserStatus.Busy) {
                await _delay.WaitAsync(2000, cancellationToken).ConfigureAwait(false);
            }

            for (int i = 0; i < 15; i++) {
                _luxaforDevice.TurnOffOrThrow();
                await _delay.WaitAsync(10, cancellationToken).ConfigureAwait(false);
                _luxaforDevice.SetColorOrThrow(PredefinedColor.Available);
                await _delay.WaitAsync(50, cancellationToken).ConfigureAwait(false);
            }

            return UserStatus.Available;
        });
    }

}
