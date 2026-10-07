#region Usings declarations

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SignalMe.Services;

#endregion

namespace SignalMe.Converters;

public static class UserStatusConverter {

    #region Statics members declarations

    /// <summary>
    ///     Every status signalme can name, with the value it prints for it, and whether the user may select it.
    /// </summary>
    /// <remarks>
    ///     The single source of truth: <see cref="TryConvert" /> parses from this table and
    ///     <see cref="KnownValues" /> is projected from it, so the accepted values and the advertised ones
    ///     cannot describe different things. "away" is in the table because it is printed (it is what the
    ///     device shows while the session is locked) but is not selectable: it is a presence override
    ///     SignalMe applies by itself, never a status the user asks for.
    /// </remarks>
    private static readonly (string Value, UserStatus Status, bool Selectable)[] _statuses = [
        ("available", UserStatus.Available, Selectable: true),
        ("free", UserStatus.Available, Selectable: true),
        ("busy", UserStatus.Busy, Selectable: true),
        ("away", UserStatus.Away, Selectable: false),
        ("do-not-disturb", UserStatus.DoNotDisturb, Selectable: true),
        ("dnd", UserStatus.DoNotDisturb, Selectable: true)
    ];

    /// <summary>
    ///     The values the user may type, aliases included, in display order.
    /// </summary>
    public static ReadOnlyCollection<string> KnownValues { get; } = new(_statuses.Where(status => status.Selectable).Select(status => status.Value).ToArray());

    /// <summary>
    ///     The value signalme prints for a status: the first one listed for it, aliases being alternatives.
    ///     Covers every status, "away" included, since it is printed even though it cannot be typed.
    /// </summary>
    public static string ToCanonicalValue(UserStatus status) {
        foreach ((string value, UserStatus candidate, _) in _statuses) {
            if (candidate == status) { return value; }
        }

        throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(UserStatus));
    }

    public static bool TryConvert(string input, [NotNullWhen(true)] out UserStatus? userStatus) {
        ArgumentNullException.ThrowIfNull(input);

        foreach ((string value, UserStatus status, bool selectable) in _statuses) {
            if (selectable && string.Equals(value, input, StringComparison.Ordinal)) {
                userStatus = status;

                return true;
            }
        }

        userStatus = null;

        return false;
    }

    #endregion

}
