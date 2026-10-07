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
///     green, and ends on the available status.
/// </summary>
/// <remarks>
///     The one pattern that deliberately changes the durable status instead of leaving it as it was.
/// </remarks>
public sealed class ReadyPattern : IMoodPattern {

    #region Fields declarations

    private readonly ILuxaforDevice _luxaforDevice;
    private readonly IDelay         _delay;

    #endregion

    #region Constructors declarations

    public ReadyPattern(ILuxaforDevice luxaforDevice, IDelay delay) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);
        ArgumentNullException.ThrowIfNull(delay);

        _luxaforDevice = luxaforDevice;
        _delay         = delay;
    }

    #endregion

    /// <inheritdoc />
    public async Task<UserStatus?> PlayAsync(UserStatus? baseStatus, CancellationToken cancellationToken) {
        UserStatus? displayedStatus = baseStatus;

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
    }

}
