#region Usings declarations

using System;

using SignalMe.Infrastructure;

using Spectre.Console.Cli;

#endregion

namespace SignalMe.Commands;

/// <summary>
///     Builds the signalme command line.
/// </summary>
/// <remarks>
///     Lives here rather than in Program.cs so that the tests can run the real command line, help and exit
///     codes included, instead of a copy of it.
/// </remarks>
public static class SignalMeCommandApp {

    #region Statics members declarations

    public static CommandApp Create() {
        CommandApp app = new();

        app.Configure(config => {
            config.SetApplicationName("signalme");

            // Without a handler, Spectre swallows an unhandled exception whole: no message on any stream,
            // and a -1 exit code. Anything reaching this point is reported before signalme gives up.
            config.SetExceptionHandler((exception, _) => {
                switch (exception) {
                    case CommandParseException:
                    case CommandRuntimeException:
                        Console.Error.WriteLine(exception.Message);
                        Console.Error.WriteLine("Type 'signalme --help' for usage.");

                        return ExitCode.UsageError;
                    case OperationCanceledException:
                        Console.Error.WriteLine("SignalMe stopped.");

                        return ExitCode.Success;
                    case DeviceCommandFailedException:
                        Console.Error.WriteLine(ErrorReporting.Describe(exception));

                        return ExitCode.DeviceError;
                    default:
                        Console.Error.WriteLine(ErrorReporting.Describe(exception));

                        return ExitCode.UnexpectedError;
                }
            });
        });

        return app;
    }

    #endregion

}
