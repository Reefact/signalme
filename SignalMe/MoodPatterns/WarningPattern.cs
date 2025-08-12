#region Usings declarations

using System;
using System.Threading;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public sealed class WarningPattern {

    #region Fields declarations

    private readonly LuxaforDevice        _luxaforDevice;
    private readonly UserStatusController _userStatusController;

    #endregion

    #region Constructors declarations

    public WarningPattern(LuxaforDevice luxaforDevice) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice        = luxaforDevice;
        _userStatusController = new UserStatusController(luxaforDevice);
    }

    #endregion

    public void Play() {
        UserStatus? currentUserStatus = _userStatusController.GetUserCurrentStatus();

        const int repeatCount   = 5;
        const int colorDuration = 100; // Durée du rouge ou du bleu (en ms)
        const int offDuration   = 1;

        BrightColor red  = BrightColor.Red;
        BrightColor blue = BrightColor.Blue;

        for (int i = 0; i < repeatCount; i++) {
            // Phase 1 : avant bleu + arrière rouge progressif
            SetFrontLeds(blue);
            RunBackSequence(red, colorDuration);
            TurnAllLedsOff();
            Thread.Sleep(offDuration);

            // Phase 2 : avant rouge + arrière bleu progressif
            SetFrontLeds(red);
            RunBackSequence(blue, colorDuration);
            TurnAllLedsOff();
            Thread.Sleep(offDuration);
        }

        _userStatusController.Display(currentUserStatus);
    }

    // Allume les LEDs 1 à 3 (avant) dans une couleur uniforme
    private void SetFrontLeds(BrightColor color) {
        for (byte i = 1; i <= 3; i++) {
            LightingCommand cmd = LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(i), color);
            _luxaforDevice.Send(cmd);
        }
    }

    // Enchaîne LED 5 puis 4 et 6 avec une couleur, sur une durée totale
    private void RunBackSequence(BrightColor color, int totalDurationMs) {
        int stepDelay = totalDurationMs / 3;

        // LED 5 centrale
        _luxaforDevice.Send(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(5), color));
        Thread.Sleep(stepDelay);

        // LED 4 et 6 en même temps
        _luxaforDevice.Send(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(4), color));
        _luxaforDevice.Send(LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(6), color));
        Thread.Sleep(stepDelay * 2); // Les 3 restent allumées
    }

    // Éteint toutes les LEDs (1 à 6)
    private void TurnAllLedsOff() {
        for (byte i = 1; i <= 6; i++) {
            LightingCommand? cmd = LightingCommand.CreateSetColorCommand(TargetedLeds.FromLuxCode(i), BrightColor.Black);
            _luxaforDevice.Send(cmd);
        }
    }

}