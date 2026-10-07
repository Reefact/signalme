#region Usings declarations

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SignalMe.Services;

#endregion

namespace SignalMe.Converters;

public static class UserMoodConverter {

    #region Statics members declarations

    /// <summary>
    ///     Every value signalme accepts at its prompt for a signal, and the mood each one means. The order
    ///     is this table's own: the prompt's help lists the signals in the spec's order, not this one.
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
    ///     The values the user may type.
    /// </summary>
    public static ReadOnlyCollection<string> KnownValues { get; } = new(_moods.Select(mood => mood.Value).ToArray());

    /// <summary>
    ///     The value signalme prints for a mood, the same one the user types.
    /// </summary>
    public static string ToCanonicalValue(UserMood mood) {
        foreach ((string value, UserMood candidate) in _moods) {
            if (candidate == mood) { return value; }
        }

        throw new InvalidEnumArgumentException(nameof(mood), (int)mood, typeof(UserMood));
    }

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
