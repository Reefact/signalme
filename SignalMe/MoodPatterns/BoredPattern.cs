#region Usings declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

/// <summary>
///     Idles through deep purples, LED by LED and out of order, then fades back to the durable status.
/// </summary>
public sealed class BoredPattern {

    private const int LedCount   = 6;
    private const int RoundCount = 25;
    private const int FrameCount = 5;

    #region Statics members declarations

    private static List<byte> GetRandomLedOrder() {
        List<byte> leds = [1, 2, 3, 4, 5, 6];
        for (int i = leds.Count - 1; i > 0; i--) {
            int swapIndex = Random.Shared.Next(i + 1);
            (leds[i], leds[swapIndex]) = (leds[swapIndex], leds[i]);
        }

        return leds;
    }

    private static BrightColor ComputeBoredColor() {
        // Hue between 260° and 290°, the purple range.
        float hue = 260f + (float)Random.Shared.NextDouble() * 30f;
        // High saturation, for deep colors rather than washed out ones.
        float saturation = 0.75f + (float)Random.Shared.NextDouble() * 0.2f;
        // Moderate to high brightness, keeping some intensity without turning pastel.
        float value = 0.5f + (float)Random.Shared.NextDouble() * 0.3f;

        Rgb rgb = new Hsv(hue, saturation, value).ToRgb();

        return BrightColor.From(rgb.Red, rgb.Green, rgb.Blue);
    }

    #endregion

    #region Fields declarations

    private readonly ILuxaforDevice       _luxaforDevice;
    private readonly UserStatusController _userStatusController;
    private readonly IDelay               _delay;

    #endregion

    #region Constructors declarations

    public BoredPattern(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null, IDelay? delay = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice, userCurrentStatus);
        _delay                = delay ?? RealDelay.Instance;
    }

    #endregion

    public Task PlayAsync(CancellationToken cancellationToken) {
        UserStatus? userCurrentStatus = _userStatusController.GetUserCurrentStatus();

        return _userStatusController.PlayAndRestoreAsync(userCurrentStatus, async () => {
            BrightColor[] currentColors = new BrightColor[LedCount];
            for (int round = 0; round < RoundCount; round++) {
                foreach (byte luxCode in GetRandomLedOrder()) {
                    BrightColor boredColor = ComputeBoredColor();
                    currentColors[luxCode - 1] = boredColor;
                    _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(luxCode), boredColor));
                    await _delay.WaitAsync(Random.Shared.Next(0, 25), cancellationToken).ConfigureAwait(false);
                }
                await _delay.WaitAsync(Random.Shared.Next(0, 100), cancellationToken).ConfigureAwait(false);
            }

            TransitionToColorPerLed(currentColors, UserStatusController.GetUserStatusColor(userCurrentStatus));
        });
    }

    /// <summary>
    ///     Brings every LED to <paramref name="targetColor" />, each starting its fade at a slightly
    ///     different frame so they do not all move at once.
    /// </summary>
    private void TransitionToColorPerLed(BrightColor[] currentColors, BrightColor targetColor) {
        List<int> ledIndices = Enumerable.Range(0, LedCount).OrderBy(_ => Random.Shared.Next()).ToList();

        // Stagger each LED's start, with a light random offset so the fades overlap.
        Dictionary<int, int> transitionStartFrame = new();
        for (int i = 0; i < LedCount; i++) {
            int startFrame = i * 2 + Random.Shared.Next(-1, 2);
            transitionStartFrame[ledIndices[i]] = Math.Max(0, Math.Min(FrameCount - 1, startFrame));
        }

        for (int frame = 0; frame < FrameCount; frame++) {
            for (int i = 0; i < LedCount; i++) {
                int         start = transitionStartFrame[i];
                BrightColor colorToApply;

                if (frame < start) {
                    // Not started yet: hold the color this LED already shows.
                    colorToApply = currentColors[i];
                } else {
                    float t = (float)(frame - start + 1) / (FrameCount - start);
                    colorToApply = currentColors[i].LerpTo(targetColor, Math.Clamp(t, 0f, 1f));
                }

                _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode((byte)(i + 1)), colorToApply));
            }
        }

        // Land every LED exactly on the target color.
        for (byte led = 1; led <= LedCount; led++) {
            _luxaforDevice.SendOrThrow(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(led), targetColor));
        }
    }

}
