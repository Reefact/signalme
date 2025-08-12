#region Usings declarations

using System;
using System.Linq;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class HappyPattern {

    #region Fields declarations

    private readonly LuxaforDevice        _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public HappyPattern(LuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice);
    }

    #endregion

    public void Play() {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        const int ledCount = 6;

        BrightColor userStatusColor = _userStatusController.GetUserStatusColor(currentUserStatus);
        BrightColor pastelColor     = userStatusColor.GetPastel();

        // ==== 1. Transition initiale vers pastel ====
        UnicornTransition(userStatusColor, pastelColor);

        // ==== 2. Wave pastel arc-en-ciel ====
        float[] baseHues  = [0, 60, 120, 180, 240, 300];
        int     waveSteps = 30;
        for (int step = 0; step < waveSteps; step++) {
            for (byte led = 1; led <= ledCount; led++) {
                int              hueIndex = (led + step) % baseHues.Length;
                float            hue      = baseHues[hueIndex];
                Hsv              hsv      = new(hue, 0.5f, 0.7f); // HSV pastel
                BrightColor      color    = ColorService.GetBrightFromHsv(hsv);
                LightingCommand? cmd      = LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(led), color);
                _luxaforDevice.Send(cmd);
            }
            Thread.Sleep(10);
        }

        // ==== 3. Retour fluide vers couleur stable ====

        UnicornTransition(pastelColor, userStatusColor);

        // ==== 4. Retour à la case départ
        _userStatusController.Display(currentUserStatus);
    }

    private void UnicornTransition(BrightColor currentColor, BrightColor targetColor) {
        const int ledCount        = 6;
        const int frameIntervalMs = 1;
        int       totalFrames     = 500;

        // Convert to RGB for interpolation
        Rgb from = currentColor.ToRgb();
        Rgb to   = targetColor.ToRgb();

        Random rng = new();

        // Décalage aléatoire pour chaque LED en nombre de frames
        int[] offsets = Enumerable.Range(0, ledCount)
                                  .Select(_ => rng.Next(5, 15)) // 50ms à 150ms = 5 à 15 frames
                                  .ToArray();

        int[] ledIndices = Enumerable.Range(0, ledCount).ToArray();
        int   step;
        for (int frame = 0; frame < totalFrames; frame += step) {
            ledIndices = ledIndices.OrderBy(_ => rng.Next()).ToArray();
            foreach (int ledIndex in ledIndices) {
                int ledStart = offsets[ledIndex];
                int ledEnd   = totalFrames - 1;

                double t = (double)(frame - ledStart) / (ledEnd - ledStart);
                t = Math.Clamp(t, 0, 1);

                // Ajout d'une petite variation sur l'intensité pour un rendu moins linéaire
                double noise = rng.NextDouble() * 0.1 - 0.05; // entre -0.05 et +0.05
                t = Math.Clamp(t + noise, 0, 1);

                byte r = (byte)(from.Red   + (to.Red   - from.Red)   * t);
                byte g = (byte)(from.Green + (to.Green - from.Green) * t);
                byte b = (byte)(from.Blue  + (to.Blue  - from.Blue)  * t);

                BrightColor     interpolated = BrightColor.From(r, g, b);
                TargetedLeds    led          = TargetedLeds.FromLuxCode((byte)(ledIndex + 1));
                LightingCommand command      = LightingCommand.CreateSetColorCommand(led, interpolated);
                _luxaforDevice.Send(command);
            }

            Thread.Sleep(frameIntervalMs);
            if (frame == totalFrames - 1) {
                step = 1;
            } else {
                step = Random.Shared.Next(97, 333);
                if (totalFrames - frame < step) {
                    step = totalFrames - frame - 1;
                }
            }
        }
    }

}