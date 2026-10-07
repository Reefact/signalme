#region Usings declarations

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using SignalMe.Converters;
using SignalMe.Infrastructure;
using SignalMe.Runtime;
using SignalMe.Services;
using SignalMe.Sessions;

#endregion

namespace SignalMe.Modes.Manual;

/// <summary>
///     The mode driven from the console: reads a line, parses it, sends the intent it means. The device and
///     the colours are the coordinator's business; this only passes on what it was told, and answers by
///     itself the two questions that need no device, "status" and "help".
/// </summary>
/// <remarks>
///     An intent is awaited before the next prompt, so that the coordinator's answer ("Status: busy",
///     "Restored: busy") lands under the command it answers rather than under the next one, and a signal
///     plays to its end before another line is read. The mode prints nothing for an outcome: the
///     coordinator already said what it did.
/// </remarks>
public sealed class ManualMode : ISignalMeMode {

    #region Statics members declarations

    /// <summary>
    ///     The block of spec §16, written out rather than built from the converters: the spec lists the
    ///     signals in its own order. The tests check it against the converters, so a value added to one of
    ///     them and forgotten here fails a test rather than the user.
    /// </summary>
    private static readonly string _helpText = string.Join(Environment.NewLine, [
        "Statuses:",
        "  available",
        "  free",
        "  busy",
        "  do-not-disturb",
        "  dnd",
        "",
        "Signals:",
        "  happy",
        "  bored",
        "  desperate",
        "  warning",
        "  alerting",
        "  ready",
        "",
        "Commands:",
        "  status",
        "  off",
        "  help",
        "",
        "Press Ctrl+C to stop SignalMe."
    ]);

    private static string Describe(UserStatus? status) {
        return status is null ? "none" : UserStatusConverter.ToCanonicalValue(status.Value);
    }

    private static string Describe(SessionState session) {
        return session switch {
            SessionState.Active => "active",
            SessionState.Locked => "locked",
            _                   => throw new InvalidEnumArgumentException(nameof(session), (int)session, typeof(SessionState))
        };
    }

    #endregion

    #region Fields declarations

    private readonly IConsole _console;

    #endregion

    #region Constructors declarations

    public ManualMode(IConsole console) {
        ArgumentNullException.ThrowIfNull(console);

        _console = console;
    }

    #endregion

    /// <inheritdoc />
    /// <remarks>
    ///     Returns when the input ends: a closed stdin is a user who left, and SignalMe stops cleanly. A
    ///     read cancelled by the runtime throws instead, which the runtime takes as the same thing.
    /// </remarks>
    public async Task RunAsync(ISignalMeContext context, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(context);

        _console.WriteLine("Commands: help");
        _console.WriteLine("Press Ctrl+C to stop.");
        _console.WriteLine(string.Empty);

        while (true) {
            _console.Write("> ");
            string? line = await _console.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) { return; }

            ManualCommand command = ManualCommandParser.Parse(line);
            await HandleAsync(command, context, cancellationToken).ConfigureAwait(false);

            // A blank line closes each exchange, so that the next prompt does not run into whatever answered
            // this one (spec §42); the start-up block ends with one for the same reason. An empty line is
            // no exchange at all (spec §11): nothing answered it, so the prompt simply comes back.
            if (command is not ManualCommand.Empty) {
                _console.WriteLine(string.Empty);
            }
        }
    }

    private async Task HandleAsync(ManualCommand command, ISignalMeContext context, CancellationToken cancellationToken) {
        switch (command) {
            case ManualCommand.Empty:
                break;
            case ManualCommand.SetStatus set:
                await context.ExecuteAsync(new SignalMeIntent.SetDesiredStatus(set.Status), cancellationToken).ConfigureAwait(false);

                break;
            case ManualCommand.PlaySignal play:
                await context.ExecuteAsync(new SignalMeIntent.PlaySignal(play.Mood), cancellationToken).ConfigureAwait(false);

                break;
            case ManualCommand.TurnOff:
                await context.ExecuteAsync(new SignalMeIntent.TurnOff(), cancellationToken).ConfigureAwait(false);

                break;
            case ManualCommand.ShowStatus:
                PrintStatus(context);

                break;
            case ManualCommand.Help:
                _console.WriteLine(_helpText);

                break;
            case ManualCommand.Unknown unknown:
                // One block: a lock reported meanwhile lands before or after both lines, never between them.
                _console.WriteLine($"Unknown command: '{unknown.Input}'.{Environment.NewLine}Type 'help' to list available commands.");

                break;
            default:
                throw new ArgumentException($"Unknown command '{command.GetType().Name}'.", nameof(command));
        }
    }

    private void PrintStatus(ISignalMeContext context) {
        // One snapshot, so the four lines describe the same moment even if a lock lands meanwhile.
        SignalMeState state = context.State;

        _console.WriteLine(string.Join(Environment.NewLine, [
            $"Mode: {context.ModeName}",
            $"Desired status: {Describe(state.DesiredStatus)}",
            $"Effective status: {EffectiveStatusConverter.ToCanonicalValue(state.Effective)}",
            $"Session: {Describe(state.Session)}"
        ]));
    }

}
