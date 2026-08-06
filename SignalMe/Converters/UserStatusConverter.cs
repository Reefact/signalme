#region Usings declarations

using System;
using System.Diagnostics.CodeAnalysis;

using SignalMe.Services;

#endregion

namespace SignalMe.Converters;

public static class UserStatusConverter {

    #region Statics members declarations

    /// <summary>
    ///     Every value <see cref="TryConvert" /> accepts, aliases included, in the order they are offered
    ///     to the user.
    /// </summary>
    /// <remarks>Kept next to the switch below so that the two cannot drift apart.</remarks>
    public static readonly string[] KnownValues = ["available", "free", "busy", "away", "do-not-disturb", "dnd"];

    public static bool TryConvert(string input, [NotNullWhen(true)] out UserStatus? userStatus) {
        ArgumentNullException.ThrowIfNull(input);
        userStatus = null;

        switch (input) {
            case "away":
                userStatus = UserStatus.Away;

                return true;
            case "available":
            case "free":
                userStatus = UserStatus.Available;

                return true;
            case "busy":
                userStatus = UserStatus.Busy;

                return true;
            case "dnd":
            case "do-not-disturb":
                userStatus = UserStatus.DoNotDisturb;

                return true;
            default:
                return false;
        }
    }

    #endregion

}