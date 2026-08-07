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
    ///     Every value the CLI accepts for a durable status, in the order they are offered to the user, and
    ///     the status each one means.
    /// </summary>
    /// <remarks>
    ///     The single source of truth: <see cref="TryConvert" /> parses from this table and
    ///     <see cref="KnownValues" /> is projected from it, so the accepted values and the advertised ones
    ///     cannot describe different things.
    /// </remarks>
    private static readonly (string Value, UserStatus Status)[] _statuses = [
        ("available", UserStatus.Available),
        ("free", UserStatus.Available),
        ("busy", UserStatus.Busy),
        ("away", UserStatus.Away),
        ("do-not-disturb", UserStatus.DoNotDisturb),
        ("dnd", UserStatus.DoNotDisturb)
    ];

    /// <summary>
    ///     The accepted values, aliases included, in display order.
    /// </summary>
    public static ReadOnlyCollection<string> KnownValues { get; } = new(_statuses.Select(status => status.Value).ToArray());

    /// <summary>
    ///     The value signalme prints for a status: the first one listed for it, aliases being alternatives.
    /// </summary>
    public static string ToCanonicalValue(UserStatus status) {
        foreach ((string value, UserStatus candidate) in _statuses) {
            if (candidate == status) { return value; }
        }

        throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(UserStatus));
    }

    public static bool TryConvert(string input, [NotNullWhen(true)] out UserStatus? userStatus) {
        ArgumentNullException.ThrowIfNull(input);

        foreach ((string value, UserStatus status) in _statuses) {
            if (string.Equals(value, input, StringComparison.Ordinal)) {
                userStatus = status;

                return true;
            }
        }

        userStatus = null;

        return false;
    }

    #endregion

}
