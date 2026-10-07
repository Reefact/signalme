#region Usings declarations

using System;
using System.ComponentModel;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;
using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

public static class MoodPatternFactory {

    #region Statics members declarations

    public static IMoodPattern Create(UserMood mood, ILuxaforDevice device, IDelay delay) {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(delay);

        return mood switch {
            UserMood.Happy     => new HappyPattern(device, delay),
            UserMood.Desperate => new DesperatePattern(device, delay),
            UserMood.Warning   => new WarningPattern(device, delay),
            UserMood.Alerting  => new AlertingPattern(device, delay),
            UserMood.Ready     => new ReadyPattern(device, delay),
            UserMood.Bored     => new BoredPattern(device, delay),
            _                  => throw new InvalidEnumArgumentException(nameof(mood), (int)mood, typeof(UserMood))
        };
    }

    #endregion

}
