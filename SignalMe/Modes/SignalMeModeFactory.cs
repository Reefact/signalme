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
    ///     The single source of truth: <see cref="TryResolve" /> and <see cref="TryCreate" /> look up this
    ///     table through the one matching rule of <see cref="Find" />, and <see cref="KnownModes" /> is
    ///     projected from it, so the usage message cannot promise a mode the factory does not create, and
    ///     the name the command line prints is always the name the factory accepted.
    /// </remarks>
    private static readonly ModeEntry[] _modes = [
        new("manual", console => new ManualMode(console))
    ];

    /// <summary>
    ///     The names <see cref="TryCreate" /> accepts, in display order.
    /// </summary>
    public static ReadOnlyCollection<string> KnownModes { get; } = new(_modes.Select(mode => mode.Name).ToArray());

    /// <summary>
    ///     Finds the name the factory advertises for what was typed, so that "Mode:" and the status report
    ///     spell the mode one way whatever spacing or capitals the user used.
    /// </summary>
    public static bool TryResolve(string name, [NotNullWhen(true)] out string? canonicalName) {
        ArgumentNullException.ThrowIfNull(name);

        canonicalName = Find(name)?.Name;

        return canonicalName is not null;
    }

    /// <summary>
    ///     Creates the mode called <paramref name="name" />, accepted exactly as <see cref="TryResolve" />
    ///     accepts it.
    /// </summary>
    public static bool TryCreate(string name, IConsole console, [NotNullWhen(true)] out ISignalMeMode? mode) {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(console);

        mode = Find(name)?.Create(console);

        return mode is not null;
    }

    /// <summary>
    ///     The one matching rule: trimmed and regardless of case, so that a value typed on the command line
    ///     does not fail on a stray space or a capital.
    /// </summary>
    private static ModeEntry? Find(string name) {
        string wanted = name.Trim();

        return Array.Find(_modes, mode => string.Equals(mode.Name, wanted, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Nested types declarations

    private sealed record ModeEntry(string Name, Func<IConsole, ISignalMeMode> Create);

    #endregion

}
