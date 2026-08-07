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

            // Show every example on the root help page rather than the first few.
            config.Settings.MaximumIndirectExamples = 16;

            // Without a handler, Spectre swallows an unhandled exception whole: no message on any stream,
            // and a -1 exit code. Anything reaching this point is reported before signalme gives up.
            config.SetExceptionHandler((exception, _) => {
                switch (exception) {
                    case OperationCanceledException:
                        Console.Error.WriteLine("Interrupted. The previous status was restored.");

                        return ExitCode.Cancelled;
                    case DeviceCommandFailedException:
                        Console.Error.WriteLine(exception.Message);

                        return ExitCode.DeviceError;
                    default:
                        Console.Error.WriteLine($"signalme: {exception.GetType().Name}: {exception.Message}");

                        return ExitCode.UnexpectedError;
                }
            });

            config.AddCommand<AsCommand>("as")
                  .WithDescription("Set a durable status, or play a temporary light signal.")
                  .WithExample("as", "available")
                  .WithExample("as", "busy")
                  .WithExample("as", "dnd")
                  .WithExample("as", "away")
                  .WithExample("as", "happy")
                  .WithExample("as", "ready");

            config.AddCommand<OffCommand>("off")
                  .WithAlias("switch-off")
                  .WithDescription("Turn every LED off and forget the durable status.")
                  .WithExample("off");

            config.AddCommand<StatusCommand>("status")
                  .WithDescription("Print the durable status signalme last set.")
                  .WithExample("status");
        });

        return app;
    }

    #endregion

}
