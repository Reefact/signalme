#region Usings declarations

using SignalMe.Commands;

using Spectre.Console.Cli;

#endregion

CommandApp app = new();

app.Configure(config => {
    config.SetApplicationName("signalme");

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