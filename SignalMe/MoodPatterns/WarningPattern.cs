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
///     Alternates red and blue between the front and back LEDs, like an emergency light, then goes back to
///     the durable status.
/// </summary>
public sealed class WarningPattern {

    private const int RepeatCount = 5;
    /// <summary>How long one side holds its color, split across the three back LEDs.</summary>
    private const int ColorDurationMs = 100;
    private const int OffDurationMs   = 1;
    private const int LedCount        = 6;

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;
    private readonly IDelay               _delay;

    #endregion

    #region Constructors declarations

    public WarningPattern(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null, IDelay? delay = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice, userCurrentStatus);
        _delay                = delay ?? RealDelay.Instance;
    }

    #endregion

    public Task PlayAsync(CancellationToken cancellationToken) {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        return _userStatusController.PlayAndRestoreAsync(currentUserStatus, async () => {
            for (int i = 0; i < RepeatCount; i++) {
                // Blue at the front while red sweeps the back...
                SetFrontLeds(BrightColor.Blue);
                await SweepBackLedsAsync(BrightColor.Red, cancellationToken).ConfigureAwait(false);
                TurnAllLedsOff();
                await _delay.WaitAsync(OffDurationMs, cancellationToken).ConfigureAwait(false);

                // ... then the other way round.
                SetFrontLeds(BrightColor.Red);
                await SweepBackLedsAsync(BrightColor.Blue, cancellationToken).ConfigureAwait(false);
                TurnAllLedsOff();
                await _delay.WaitAsync(OffDurationMs, cancellationToken).ConfigureAwait(false);
            }
        });
    }

    /// <summary>Lights LEDs 1 to 3, the front of the device, in a single color.</summary>
    private void SetFrontLeds(BrightColor color) {
        for (byte led = 1; led <= 3; led++) {
            _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(led), color));
        }
    }

    /// <summary>Lights the middle back LED, then the two beside it, spreading the color over the duration.</summary>
    private async Task SweepBackLedsAsync(BrightColor color, CancellationToken cancellationToken) {
        int stepDelay = ColorDurationMs / 3;

        _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(5), color));
        await _delay.WaitAsync(stepDelay, cancellationToken).ConfigureAwait(false);

        _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(4), color));
        _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(6), color));
        await _delay.WaitAsync(stepDelay * 2, cancellationToken).ConfigureAwait(false);
    }

    private void TurnAllLedsOff() {
        for (byte led = 1; led <= LedCount; led++) {
            _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(led), BrightColor.Black));
        }
    }

}
