#region Usings declarations

using System.ComponentModel;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Runtime;

#endregion

namespace SignalMe.Services;

/// <summary>
///     The colour the device shows for a status. One table for the durable statuses and the effective ones,
///     so that an animation fading from "busy" and the coordinator rendering "busy" cannot disagree.
/// </summary>
public static class StatusColors {

    #region Statics members declarations

    /// <summary>The colour of a durable status; black, that is off, when there is none.</summary>
    public static BrightColor For(UserStatus? status) {
        return status switch {
            null                    => BrightColor.Black,
            UserStatus.Away         => PredefinedColor.Away,
            UserStatus.Available    => PredefinedColor.Available,
            UserStatus.Busy         => PredefinedColor.Busy,
            UserStatus.DoNotDisturb => PredefinedColor.DoNotDisturb,
            _                       => throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(UserStatus))
        };
    }

    /// <summary>The colour of what the device effectively shows.</summary>
    public static BrightColor For(EffectiveStatus status) {
        return status switch {
            EffectiveStatus.Off          => BrightColor.Black,
            EffectiveStatus.Away         => PredefinedColor.Away,
            EffectiveStatus.Available    => PredefinedColor.Available,
            EffectiveStatus.Busy         => PredefinedColor.Busy,
            EffectiveStatus.DoNotDisturb => PredefinedColor.DoNotDisturb,
            _                            => throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(EffectiveStatus))
        };
    }

    #endregion

}
