#region Usings declarations

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SignalMe.Services;

#endregion

namespace SignalMe.Converters;

public static class UserMoodConverter {

    #region Statics members declarations

    /// <summary>
    ///     Every value the CLI accepts for a temporary mood, in the order they are offered to the user, and
    ///     the mood each one means.
    /// </summary>
    /// <remarks>
    ///     The single source of truth: <see cref="TryConvert" /> parses from this table and
    ///     <see cref="KnownValues" /> is projected from it, so the accepted values and the advertised ones
    ///     cannot describe different things.
    /// </remarks>
    private static readonly (string Value, UserMood Mood)[] _moods = [
        ("happy", UserMood.Happy),
        ("bored", UserMood.Bored),
        ("desperate", UserMood.Desperate),
        ("ready", UserMood.Ready),
        ("warning", UserMood.Warning),
        ("alerting", UserMood.Alerting)
    ];

    /// <summary>
    ///     The accepted values, in display order.
    /// </summary>
    public static ReadOnlyCollection<string> KnownValues { get; } = new(_moods.Select(mood => mood.Value).ToArray());

    public static bool TryConvert(string input, [NotNullWhen(true)] out UserMood? userMood) {
        ArgumentNullException.ThrowIfNull(input);

        foreach ((string value, UserMood mood) in _moods) {
            if (string.Equals(value, input, StringComparison.Ordinal)) {
                userMood = mood;

                return true;
            }
        }

        userMood = null;

        return false;
    }

    #endregion

}
