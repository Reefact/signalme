#region Usings declarations

using System;
using System.Threading.Tasks;

using SignalMe.Infrastructure;

#endregion

namespace SignalMe.Commands;

/// <summary>
///     The signalme command line: reads the arguments, answers <c>--help</c> and <c>--version</c>, rejects
///     a usage error, or runs <see cref="RunCommand" />, and turns whatever happened into an exit code.
/// </summary>
/// <remarks>
///     Lives here rather than in Program.cs so that the tests can run the real command line, help and exit
///     codes included, instead of a copy of it. All of its output, including help and version
///     information, goes through the <see cref="IConsole" /> of the services.
/// </remarks>
public static class SignalMeCommandApp {

    private const string UsageHint = "Type 'signalme --help' for usage.";

    #region Statics members declarations

    /// <summary>
    ///     Runs the command line over <paramref name="services" />, or over the real console, device
    ///     discovery, status store and monitors when none are given, and returns the exit code.
    /// </summary>
    public static async Task<int> RunAsync(string[] args, SignalMeServices? services = null) {
        ArgumentNullException.ThrowIfNull(args);

        SignalMeServices resolved = services ?? new SignalMeServices();
        IConsole         console  = resolved.Console;

        try {
            CommandLineRequest request = CommandLineParser.Parse(args);

            switch (request) {
                case CommandLineRequest.ShowHelp:
                    console.WriteLine(CommandLineParser.HelpText(RunCommand.Version));

                    return ExitCode.Success;
                case CommandLineRequest.ShowVersion:
                    console.WriteLine(RunCommand.Version);

                    return ExitCode.Success;
                case CommandLineRequest.Invalid invalid:
                    console.WriteError(invalid.Reason);
                    console.WriteError(UsageHint);

                    return ExitCode.UsageError;
                case CommandLineRequest.Run run:
                    return await new RunCommand(resolved).ExecuteAsync(run.Mode).ConfigureAwait(false);
                default:
                    throw new ArgumentException($"Unknown command-line request '{request.GetType().Name}'.", nameof(args));
            }
        } catch (OperationCanceledException) {
            // A device selection cut short by Ctrl+C or by the end of input: a clean stop, not a failure,
            // printed on the same stream and in the same words as the runtime's own stop.
            console.WriteLine("SignalMe stopped.");

            return ExitCode.Success;
        } catch (DeviceCommandFailedException exception) {
            console.WriteError(ErrorReporting.Describe(exception));

            return ExitCode.DeviceError;
        } catch (Exception exception) {
            console.WriteError(ErrorReporting.Describe(exception));

            return ExitCode.UnexpectedError;
        }
    }

    #endregion

}
