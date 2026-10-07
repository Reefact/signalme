#region Usings declarations

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

using SignalMe.Infrastructure;

#endregion

namespace SignalMe.Modes;

/// <summary>
///     The modes signalme can run, by name.
/// </summary>
public static class SignalMeModeFactory {

    #region Statics members declarations

    /// <summary>
    ///     The names <see cref="TryCreate" /> accepts, in display order. Advertised and accepted from the
    ///     same list, so the usage message cannot promise a mode the factory does not know.
    /// </summary>
    public static ReadOnlyCollection<string> KnownModes { get; } = new(["manual"]);

    /// <summary>
    ///     Creates the mode called <paramref name="name" />, compared trimmed and regardless of case, so that
    ///     a value typed on the command line does not fail on a stray space or a capital.
    /// </summary>
    public static bool TryCreate(string name, IConsole console, [NotNullWhen(true)] out ISignalMeMode? mode) {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(console);

        // The manual lot wires "manual" to ManualMode here (matched on name.Trim() with
        // StringComparison.OrdinalIgnoreCase) and adds a test creating every name of KnownModes, " MANUAL "
        // included, so that the usage message can never promise a mode this method does not create. Until
        // then no mode can be created.
        mode = null;

        return false;
    }

    #endregion

}
