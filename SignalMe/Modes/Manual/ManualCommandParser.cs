#region Usings declarations

using System;

using SignalMe.Converters;
using SignalMe.Services;

#endregion

namespace SignalMe.Modes.Manual;

/// <summary>
///     Turns a line typed at the prompt into a <see cref="ManualCommand" />. A pure function, so it is
///     tested on its own, without a console or a coordinator.
/// </summary>
public static class ManualCommandParser {

    #region Statics members declarations

    /// <summary>
    ///     Surrounding spaces and the case are forgiven, since a prompt is typed by hand. The statuses and
    ///     the signals come from the converters, so the manual mode accepts exactly the values the rest of
    ///     the tool advertises; "away" is not among them, because the session lock applies it and the user
    ///     does not.
    /// </summary>
    public static ManualCommand Parse(string line) {
        ArgumentNullException.ThrowIfNull(line);

        string input = line.Trim();
        if (input.Length == 0) { return new ManualCommand.Empty(); }

        string word = input.ToLowerInvariant();
        if (UserStatusConverter.TryConvert(word, out UserStatus? status)) { return new ManualCommand.SetStatus(status.Value); }
        if (UserMoodConverter.TryConvert(word, out UserMood? mood)) { return new ManualCommand.PlaySignal(mood.Value); }

        return word switch {
            "status" => new ManualCommand.ShowStatus(),
            "off"    => new ManualCommand.TurnOff(),
            "help"   => new ManualCommand.Help(),
            _        => new ManualCommand.Unknown(input)
        };
    }

    #endregion

}
