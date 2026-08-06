#region Usings declarations

using System;
using System.Diagnostics.CodeAnalysis;

using SignalMe.Services;

#endregion

namespace SignalMe.Converters;

public static class UserMoodConverter {

    #region Statics members declarations

    /// <summary>
    ///     Every value <see cref="TryConvert" /> accepts, in the order they are offered to the user.
    /// </summary>
    /// <remarks>Kept next to the switch below so that the two cannot drift apart.</remarks>
    public static readonly string[] KnownValues = ["happy", "bored", "desperate", "ready", "warning", "alerting"];

    public static bool TryConvert(string input, [NotNullWhen(true)] out UserMood? userMood) {
        ArgumentNullException.ThrowIfNull(input);
        userMood = null;

        switch (input) {
            case "alerting":
                userMood = UserMood.Alerting;

                return true;
            case "warning":
                userMood = UserMood.Warning;

                return true;
            case "desperate":
                userMood = UserMood.Desperate;

                return true;
            case "happy":
                userMood = UserMood.Happy;

                return true;
            case "ready":
                userMood = UserMood.Ready;

                return true;
            case "bored":
                userMood = UserMood.Bored;

                return true;
            default:
                return false;
        }
    }

    #endregion

}