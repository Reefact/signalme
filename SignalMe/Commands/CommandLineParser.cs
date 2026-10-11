#region Usings declarations

using System;
using System.Collections.Generic;

using SignalMe.Modes;

#endregion

namespace SignalMe.Commands;

/// <summary>
///     Reads the arguments signalme was started with: one option, <c>--mode</c>, plus <c>--help</c> and
///     <c>--version</c>.
/// </summary>
/// <remarks>
///     <para>
///         Written by hand rather than with a command-line library: SignalMe has a single option and no
///         sub-commands, and the library it used to rely on added more overhead than value. That library
///         wrote its messages and help through its own console, which the tests could not observe, and it
///         reported a stray argument as an unknown sub-command. Here every message is SignalMe's own, and
///         all output goes through <see cref="Infrastructure.IConsole" />.
///     </para>
///     <para>
///         <c>--help</c> and <c>--version</c> take precedence over everything else on the command line, even
///         when it also contains an error: they have no side effects, and someone asking for help with a
///         command line they got wrong should get it. Otherwise, only the first problem encountered is
///         reported. If <c>--mode</c> is given more than once, the last value wins, as is usual for a
///         repeated option.
///     </para>
///     <para>
///         A mode name never starts with a dash. An empty value, or one that starts with <c>-</c>, is
///         therefore treated as a missing value, whether it follows the option as a separate argument
///         (<c>--mode -x</c>) or after an equals sign (<c>--mode=-x</c>).
///     </para>
/// </remarks>
public static class CommandLineParser {

    /// <summary>The mode SignalMe runs in when the command line names none.</summary>
    public const string DefaultMode = "manual";

    #region Statics members declarations

    public static CommandLineRequest Parse(IReadOnlyList<string> args) {
        ArgumentNullException.ThrowIfNull(args);

        bool    help           = false;
        bool    version        = false;
        bool    afterSeparator = false;
        string? mode           = null;
        string? problem        = null;

        for (int index = 0; index < args.Count; index++) {
            string argument = args[index];

            // After "--" nothing is treated as an option, and SignalMe accepts no positional arguments.
            if (afterSeparator) {
                problem ??= $"Unexpected argument: '{argument}'.";

                continue;
            }

            switch (argument) {
                case "--":
                    afterSeparator = true;

                    break;
                case "-h" or "--help":
                    help = true;

                    break;
                case "-v" or "--version":
                    version = true;

                    break;
                case "-m" or "--mode":
                    // What follows is the value only if it can be one; otherwise it is left for the next
                    // iteration, which reports it if nothing was reported yet.
                    if (index + 1 < args.Count && IsModeValue(args[index + 1])) {
                        mode = args[++index];
                    } else {
                        problem ??= MissingValue(argument);
                    }

                    break;
                default:
                    if (TrySplitInlineValue(argument, out string option, out string value)) {
                        if (IsModeValue(value)) {
                            mode = value;
                        } else {
                            problem ??= MissingValue(option);
                        }
                    } else if (IsOption(argument)) {
                        problem ??= $"Unknown option: '{OptionName(argument)}'.";
                    } else {
                        problem ??= $"Unexpected argument: '{argument}'.";
                    }

                    break;
            }
        }

        if (help) { return new CommandLineRequest.ShowHelp(); }
        if (version) { return new CommandLineRequest.ShowVersion(); }
        if (problem is not null) { return new CommandLineRequest.Invalid(problem); }

        return new CommandLineRequest.Run(mode ?? DefaultMode);
    }

    /// <summary>
    ///     The usage, as <c>--help</c> prints it. Built from the mode factory, so a new mode is listed
    ///     without anyone having to remember this text.
    /// </summary>
    public static string HelpText(string version) {
        ArgumentNullException.ThrowIfNull(version);

        return string.Join(Environment.NewLine,
                           $"SignalMe {version}",
                           "A resident presence runtime for Luxafor devices.",
                           string.Empty,
                           "Usage:",
                           "  signalme [--mode <MODE>]",
                           "  signalme --help",
                           "  signalme --version",
                           string.Empty,
                           "Options:",
                           "  -m, --mode <MODE>  Where the status comes from. Default: " + DefaultMode + ".",
                           "                     Available modes: " + string.Join(", ", SignalMeModeFactory.KnownModes) + ".",
                           "  -h, --help         Prints this help.",
                           "  -v, --version      Prints the version.",
                           string.Empty,
                           "Once SignalMe runs, type 'help' at its prompt for the commands it understands.");
    }

    private static bool IsOption(string argument) {
        return argument.Length > 1 && argument[0] == '-';
    }

    /// <summary>Whether <paramref name="candidate" /> can be a mode name: not empty, and not starting with a dash.</summary>
    private static bool IsModeValue(string candidate) {
        return candidate.Length > 0 && candidate[0] != '-';
    }

    private static string OptionName(string argument) {
        int equals = argument.IndexOf('=', StringComparison.Ordinal);

        return equals < 0 ? argument : argument[..equals];
    }

    /// <summary>Reads <c>--mode=VALUE</c> and <c>-m=VALUE</c>; anything else is not an inline value.</summary>
    private static bool TrySplitInlineValue(string argument, out string option, out string value) {
        foreach (string name in (string[])["--mode", "-m"]) {
            if (argument.StartsWith(name + "=", StringComparison.Ordinal)) {
                option = name;
                value  = argument[(name.Length + 1)..];

                return true;
            }
        }

        option = string.Empty;
        value  = string.Empty;

        return false;
    }

    private static string MissingValue(string option) {
        return $"Option '{option}' needs a value: {option} <MODE>.";
    }

    #endregion

}
