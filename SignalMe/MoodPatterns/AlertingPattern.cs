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
///     Flashes red twenty times, fast.
/// </summary>
public sealed class AlertingPattern : IMoodPattern {

    #region Fields declarations

    private readonly ILuxaforDevice _luxaforDevice;
    private readonly IDelay         _delay;

    #endregion

    #region Constructors declarations

    public AlertingPattern(ILuxaforDevice luxaforDevice, IDelay delay) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);
        ArgumentNullException.ThrowIfNull(delay);

        _luxaforDevice = luxaforDevice;
        _delay         = delay;
    }

    #endregion

    /// <inheritdoc />
    public async Task<UserStatus?> PlayAsync(UserStatus? baseStatus, CancellationToken cancellationToken) {
        for (int i = 0; i < 20; i++) {
            _luxaforDevice.SetColorOrThrow(BrightColor.Red);
            await _delay.WaitAsync(50, cancellationToken).ConfigureAwait(false);
            _luxaforDevice.TurnOffOrThrow();
            await _delay.WaitAsync(50, cancellationToken).ConfigureAwait(false);
        }

        return baseStatus;
    }

}
