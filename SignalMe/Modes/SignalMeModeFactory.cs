#region Usings declarations

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using SignalMe.Infrastructure;
using SignalMe.Modes.Manual;

#endregion

namespace SignalMe.Modes;

/// <summary>
///     The modes signalme can run, by name.
/// </summary>
public static class SignalMeModeFactory {

    #region Statics members declarations

    /// <summary>
    ///     Every mode by name, with how to build it, in display order.
    /// </summary>
    /// <remarks>
    ///     The single source of truth: <see cref="TryCreate" /> looks up this table and
    ///     <see cref="KnownModes" /> is projected from it, so the usage message cannot promise a mode the
    ///     factory does not create.
    /// </remarks>
    private static readonly (string Name, Func<IConsole, ISignalMeMode> Create)[] _modes = [
        ("manual", console => new ManualMode(console))
    ];

    /// <summary>
    ///     The names <see cref="TryCreate" /> accepts, in display order.
    /// </summary>
    public static ReadOnlyCollection<string> KnownModes { get; } = new(_modes.Select(mode => mode.Name).ToArray());

    /// <summary>
    ///     Creates the mode called <paramref name="name" />, compared trimmed and regardless of case, so that
    ///     a value typed on the command line does not fail on a stray space or a capital.
    /// </summary>
    public static bool TryCreate(string name, IConsole console, [NotNullWhen(true)] out ISignalMeMode? mode) {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(console);

        string wanted = name.Trim();
        foreach ((string candidate, Func<IConsole, ISignalMeMode> create) in _modes) {
            if (string.Equals(candidate, wanted, StringComparison.OrdinalIgnoreCase)) {
                mode = create(console);

                return true;
            }
        }

        mode = null;

        return false;
    }

    #endregion

}
