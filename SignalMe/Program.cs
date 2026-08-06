#region Usings declarations

using System;

using SignalMe.Commands;
using SignalMe.Infrastructure;

using Spectre.Console.Cli;

#endregion

CommandApp app = new();

app.Configure(config => {
    config.SetApplicationName("signalme");

    // Without a handler, Spectre swallows an unhandled exception whole: no message on any stream, and a
    // -1 exit code. Anything reaching this point is reported before signalme gives up.
    config.SetExceptionHandler((exception, _) => {
        if (exception is DeviceCommandFailedException) {
            Console.Error.WriteLine(exception.Message);

            return ExitCode.DeviceError;
        }

        Console.Error.WriteLine($"signalme: {exception.GetType().Name}: {exception.Message}");

        return ExitCode.UnexpectedError;
    });

    config.AddCommand<AsCommand>("as")
          .WithDescription("Sets a light-based status indicator.")
          .WithExample("as", "available")
          .WithExample("as", "busy")
          .WithExample("as", "do-not-disturb")
          .WithExample("as", "away")
          .WithExample("as", "happy")
          .WithExample("as", "ready")
          .WithExample("as", "warning")
          .WithExample("as", "alerting")
          .WithExample("as", "bored")
          .WithExample("as", "desperate");

    config.AddCommand<OffCommand>("switch-off")
          .WithDescription("Turn off all LEDs");
});

return app.Run(args);
