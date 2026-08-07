#region Usings declarations

using System;

using SignalMe.Converters;
using SignalMe.Infrastructure;
using SignalMe.Services;

using Spectre.Console.Cli;

#endregion

namespace SignalMe.Commands;

/// <summary>
///     Prints the durable status signalme last set.
/// </summary>
/// <remarks>
///     Deliberately does not open the device: signalme cannot read the LEDs back, so this reports what it
///     remembers having asked for, not what the device is physically showing. It also means the command
///     works with the device unplugged.
/// </remarks>
public sealed class StatusCommand : Command {

    #region Statics members declarations

    public static string Describe(UserStatus? status) {
        return status is null
                   ? "No durable status is currently set."
                   : UserStatusConverter.ToCanonicalValue(status.Value);
    }

    #endregion

    public override int Execute(CommandContext context) {
        ArgumentNullException.ThrowIfNull(context);

        Console.WriteLine(Describe(UserCurrentStatus.Default.Get()));

        return ExitCode.Success;
    }

}
