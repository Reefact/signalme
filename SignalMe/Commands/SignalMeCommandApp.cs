#region Usings declarations

using System;

using SignalMe.Infrastructure;

using Spectre.Console;
using Spectre.Console.Cli;

#endregion

namespace SignalMe.Commands;

/// <summary>
///     Builds the signalme command line: one default command, <see cref="RunCommand" />, and the help and
///     version Spectre provides around it.
/// </summary>
/// <remarks>
///     Lives here rather than in Program.cs so that the tests can run the real command line, help and exit
///     codes included, instead of a copy of it.
/// </remarks>
public static class SignalMeCommandApp {

    #region Statics members declarations

    /// <summary>
    ///     Creates the command line over <paramref name="services" />, or over the real console, device
    ///     discovery, status store and session monitor when none are given.
    /// </summary>
    public static CommandApp<RunCommand> Create(SignalMeServices? services = null) {
        SignalMeServices       resolved = services ?? new SignalMeServices();
        CommandApp<RunCommand> app      = new();

        app.WithData(resolved);
        app.Configure(config => {
            config.SetApplicationName("signalme");

            // Enables --version and -v, which Spectre answers by itself before parsing anything else. It
            // renders the string as markup, hence the escape; the version is the one the banner prints.
            config.SetApplicationVersion(Markup.Escape(RunCommand.Version));

            // Parsing stays relaxed, Spectre's default. In strict mode a failed parse is retried with the
            // default command's name inserted among the arguments, so "signalme --mode" would bind the
            // mode to "__default_command" instead of failing. Relaxed, an unknown option or a stray
            // argument lands in the command's remaining arguments, and RunCommand rejects them first thing.

            // Without a handler, Spectre swallows an unhandled exception whole: no message on any stream,
            // and a -1 exit code. The console is the captured one rather than anything from the resolver
            // the handler is given, which is null for a parse error.
            config.SetExceptionHandler((exception, _) => {
                IConsole console = resolved.Console;
                switch (exception) {
                    case CommandParseException:
                    case CommandRuntimeException:
                        console.WriteError(exception.Message);
                        console.WriteError("Type 'signalme --help' for usage.");

                        return ExitCode.UsageError;
                    case OperationCanceledException:
                        // A clean stop, not a failure: the same line the runtime prints, on the same stream.
                        console.WriteLine("SignalMe stopped.");

                        return ExitCode.Success;
                    case DeviceCommandFailedException:
                        console.WriteError(ErrorReporting.Describe(exception));

                        return ExitCode.DeviceError;
                    default:
                        console.WriteError(ErrorReporting.Describe(exception));

                        return ExitCode.UnexpectedError;
                }
            });
        });

        return app;
    }

    #endregion

}
