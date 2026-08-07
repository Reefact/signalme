#region Usings declarations

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

/// <summary>
///     Fades to a pastel version of the durable status color, runs a pastel rainbow wave, then fades back.
/// </summary>
public sealed class HappyPattern {

    private const int LedCount        = 6;
    private const int WaveSteps       = 30;
    private const int FrameIntervalMs = 1;
    private const int TotalFrames     = 500;

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;
    private readonly IDelay               _delay;

    #endregion

    #region Constructors declarations

    public HappyPattern(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null, IDelay? delay = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice, userCurrentStatus);
        _delay                = delay ?? RealDelay.Instance;
    }

    #endregion

    public Task PlayAsync(CancellationToken cancellationToken) {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        return _userStatusController.PlayAndRestoreAsync(currentUserStatus, async () => {
            BrightColor userStatusColor = UserStatusController.GetUserStatusColor(currentUserStatus);
            BrightColor pastelColor     = userStatusColor.GetPastel();

            await FadeAsync(userStatusColor, pastelColor, cancellationToken).ConfigureAwait(false);
            await PlayRainbowWaveAsync(cancellationToken).ConfigureAwait(false);
            await FadeAsync(pastelColor, userStatusColor, cancellationToken).ConfigureAwait(false);
        });
    }

    private async Task PlayRainbowWaveAsync(CancellationToken cancellationToken) {
        float[] baseHues = [0, 60, 120, 180, 240, 300];

        for (int step = 0; step < WaveSteps; step++) {
            for (byte led = 1; led <= LedCount; led++) {
                float       hue   = baseHues[(led + step) % baseHues.Length];
                BrightColor color = ColorService.GetBrightFromHsv(new Hsv(hue, 0.5f, 0.7f));
                _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(led), color));
            }
            await _delay.WaitAsync(10, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Interpolates every LED from one color to another, each LED starting a little later than the last
    ///     and drifting slightly, so the fade never looks mechanical.
    /// </summary>
    private async Task FadeAsync(BrightColor currentColor, BrightColor targetColor, CancellationToken cancellationToken) {
        Rgb from = currentColor.ToRgb();
        Rgb to   = targetColor.ToRgb();

        // Per-LED head start, expressed in frames.
        int[] offsets    = Enumerable.Range(0, LedCount).Select(_ => Random.Shared.Next(5, 15)).ToArray();
        int[] ledIndices = Enumerable.Range(0, LedCount).ToArray();

        int step;
        for (int frame = 0; frame < TotalFrames; frame += step) {
            ledIndices = ledIndices.OrderBy(_ => Random.Shared.Next()).ToArray();
            foreach (int ledIndex in ledIndices) {
                int    ledStart = offsets[ledIndex];
                double t        = Math.Clamp((double)(frame - ledStart) / (TotalFrames - 1 - ledStart), 0, 1);

                // A touch of noise on the intensity, so the LEDs do not move in lockstep.
                t = Math.Clamp(t + (Random.Shared.NextDouble() * 0.1 - 0.05), 0, 1);

                byte r = (byte)(from.Red   + (to.Red   - from.Red)   * t);
                byte g = (byte)(from.Green + (to.Green - from.Green) * t);
                byte b = (byte)(from.Blue  + (to.Blue  - from.Blue)  * t);

                _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode((byte)(ledIndex + 1)), BrightColor.From(r, g, b)));
            }

            await _delay.WaitAsync(FrameIntervalMs, cancellationToken).ConfigureAwait(false);

            if (frame == TotalFrames - 1) {
                step = 1;
            } else {
                step = Random.Shared.Next(97, 333);
                if (TotalFrames - frame < step) { step = TotalFrames - frame - 1; }
            }
        }
    }

}
