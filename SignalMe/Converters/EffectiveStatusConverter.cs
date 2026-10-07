#region Usings declarations

using System.ComponentModel;

using SignalMe.Runtime;
using SignalMe.Services;

#endregion

namespace SignalMe.Converters;

/// <summary>
///     Names what the device shows, the way the status it comes from is named: "Effective status: busy"
///     and "Status: busy" must spell the status alike, and "off" is the one word for a dark device.
/// </summary>
/// <remarks>
///     Shared by the coordinator, which announces what it rendered, and the manual mode, which answers
///     "status": one table, so the two cannot drift apart.
/// </remarks>
public static class EffectiveStatusConverter {

    #region Statics members declarations

    public static string ToCanonicalValue(EffectiveStatus status) {
        return status switch {
            EffectiveStatus.Off          => "off",
            EffectiveStatus.Away         => UserStatusConverter.ToCanonicalValue(UserStatus.Away),
            EffectiveStatus.Available    => UserStatusConverter.ToCanonicalValue(UserStatus.Available),
            EffectiveStatus.Busy         => UserStatusConverter.ToCanonicalValue(UserStatus.Busy),
            EffectiveStatus.DoNotDisturb => UserStatusConverter.ToCanonicalValue(UserStatus.DoNotDisturb),
            _                            => throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(EffectiveStatus))
        };
    }

    #endregion

}
