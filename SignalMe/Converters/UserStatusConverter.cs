#region Usings declarations

using System;
using System.Diagnostics.CodeAnalysis;

using SignalMe.Services;

#endregion

namespace SignalMe.Converters;

public static class UserStatusConverter {

    #region Statics members declarations

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