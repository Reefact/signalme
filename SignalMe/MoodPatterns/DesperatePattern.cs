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
///     Blinks S-O-S in white, then goes back to the durable status.
/// </summary>
public sealed class DesperatePattern {

    private const int ShortBlinkMs = 100;
    private const int LongBlinkMs  = 500;
    private const int GapMs        = 300;

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;
    private readonly IDelay               _delay;

    #endregion

    #region Constructors declarations

    public DesperatePattern(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null, IDelay? delay = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice, userCurrentStatus);
        _delay                = delay ?? RealDelay.Instance;
    }

    #endregion

    public Task PlayAsync(CancellationToken cancellationToken) {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        return _userStatusController.PlayAndRestoreAsync(currentUserStatus, async () => {
            await _delay.WaitAsync(1000, cancellationToken).ConfigureAwait(false);
            _luxaforDevice.TurnOffOrThrow();
            await _delay.WaitAsync(GapMs, cancellationToken).ConfigureAwait(false);

            await BlinkAsync(3, ShortBlinkMs, cancellationToken).ConfigureAwait(false); // S
            await _delay.WaitAsync(GapMs, cancellationToken).ConfigureAwait(false);
            await BlinkAsync(3, LongBlinkMs, cancellationToken).ConfigureAwait(false); // O
            await _delay.WaitAsync(GapMs, cancellationToken).ConfigureAwait(false);
            await BlinkAsync(3, ShortBlinkMs, cancellationToken).ConfigureAwait(false); // S

            await _delay.WaitAsync(1000, cancellationToken).ConfigureAwait(false);
        });
    }

    private async Task BlinkAsync(int count, int onDurationMs, CancellationToken cancellationToken) {
        for (int i = 0; i < count; i++) {
            _luxaforDevice.SetColorOrThrow(BrightColor.White);
            await _delay.WaitAsync(onDurationMs, cancellationToken).ConfigureAwait(false);
            _luxaforDevice.TurnOffOrThrow();
            if (i < count - 1) { await _delay.WaitAsync(ShortBlinkMs, cancellationToken).ConfigureAwait(false); }
        }
    }

}
