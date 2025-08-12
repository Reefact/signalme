#region Usings declarations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class BoredPattern {

    #region Statics members declarations

    private static IEnumerable<byte> GetRandomLedOrder() {
        List<byte> leds = [1, 2, 3, 4, 5, 6];
        Random     rng  = new();
        for (int i = leds.Count - 1; i > 0; i--) {
            int swapIndex = rng.Next(i + 1);
            (leds[i], leds[swapIndex]) = (leds[swapIndex], leds[i]);
        }

        return leds;
    }

    #endregion

    #region Fields declarations

    private readonly LuxaforDevice        _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public BoredPattern(LuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice);
    }

    #endregion

    public void Play() {
        UserStatus? userCurrentStatus = _userStatusController.GetUserCurrentStatus();

        BrightColor[] currentColors = new BrightColor[6];
        for (int i = 0; i < 25; i++) {
            IEnumerable<byte> ledCodes = GetRandomLedOrder();
            foreach (byte luxCode in ledCodes) {
                TargetedLeds targetedLed = TargetedLeds.FromLuxCode(luxCode);
                BrightColor  boredColor  = ComputeBoredColor();
                currentColors[luxCode - 1] = boredColor;
                _luxaforDevice.Send(LightingCommand.CreateSetColorCommand(targetedLed, boredColor));
                Thread.Sleep(Random.Shared.Next(0, 25));
            }
            Thread.Sleep(Random.Shared.Next(0, 100));
        }

        BrightColor targetColor = _userStatusController.GetUserStatusColor(userCurrentStatus);
        TransitionToColorPerLed(currentColors, targetColor);

        _userStatusController.Display(userCurrentStatus);
    }

    public void TransitionToColorPerLed(BrightColor[] currentColors, BrightColor targetColor) {
        const int ledCount   = 6;
        const int frameCount = 5;

        // Tirage aléatoire de l’ordre de transition
        List<int> ledIndices = Enumerable.Range(0, ledCount).OrderBy(_ => Random.Shared.Next()).ToList();

        // Pour chaque LED, on détermine son frame de début de transition
        Dictionary<int, int> transitionStartFrame = new();
        for (int i = 0; i < ledCount; i++) {
            // Étage la transition dans les 10 frames avec un décalage aléatoire léger
            int startFrame = i * 2 + Random.Shared.Next(-1, 2); // entre 0 et ~10, avec chevauchement possible
            transitionStartFrame[ledIndices[i]] = Math.Max(0, Math.Min(frameCount - 1, startFrame));
        }

        for (int frame = 0; frame < frameCount; frame++) {
            for (int i = 0; i < ledCount; i++) {
                int         led   = i + 1;
                int         start = transitionStartFrame[i];
                BrightColor colorToApply;

                if (frame < start) {
                    // Pas encore commencé, on garde la couleur actuelle
                    colorToApply = currentColors[i];
                } else {
                    float t = (float)(frame - start + 1) / (frameCount - start);
                    t = Math.Clamp(t, 0f, 1f);

                    colorToApply = currentColors[i].LerpTo(targetColor, t);
                }

                TargetedLeds ledTarget = TargetedLeds.FromLuxCode((byte)led);
                _luxaforDevice.Send(LightingCommand.CreateSetColorCommand(ledTarget, colorToApply));
            }

            // Thread.Sleep(delayPerFrameMs);
        }

        // Finalisation : appliquer la couleur cible à toutes les LEDs
        for (byte led = 1; led <= ledCount; led++) {
            _luxaforDevice.Send(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(led), targetColor));
        }
    }

    private BrightColor ComputeBoredColor() {
        Random random = Random.Shared;
        // Hue entre 260° et 290° (teintes violettes)
        float hue = 260f + (float)random.NextDouble() * 30f;

        // Saturation élevée (0.75 à 0.95) pour des couleurs profondes
        float saturation = 0.75f + (float)random.NextDouble() * 0.2f;

        // Value modérée à élevée (0.5 à 0.8) pour garder une bonne intensité sans pastel
        float value = 0.5f + (float)random.NextDouble() * 0.3f;

        Hsv hsv = new(hue, saturation, value);
        Rgb rgb = hsv.ToRgb();

        return BrightColor.From(rgb.Red, rgb.Green, rgb.Blue);
    }

}