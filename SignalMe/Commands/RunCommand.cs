#region Usings declarations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Devices;
using SignalMe.Infrastructure;
using SignalMe.Modes;
using SignalMe.Runtime;
using SignalMe.Sessions;

using Spectre.Console.Cli;

#endregion

namespace SignalMe.Commands;

/// <summary>
///     The one command of signalme: starts the resident runtime in the requested mode and turns how it
///     ended into an exit code.
/// </summary>
/// <remarks>
///     <para>
///         Its dependencies come from the <see cref="SignalMeServices" /> the command app attached to the
///         context, so the tests run this very command over a scripted console and fake devices. The
///         constructor stays parameterless for the same reason: Spectre's own resolver builds the command.
///     </para>
///     <para>
///         The steps are ordered so that the command line is rejected before a device is touched, and so
///         that nothing is read from the console before a device is selected: the CI agent has no device
///         and no input, and must stop on the device error rather than on a read that never completes.
///     </para>
/// </remarks>
public sealed class RunCommand : AsyncCommand<RunCommand.Settings> {

    private const string UsageHint = "Type 'signalme --help' for usage.";

    #region Statics members declarations

    /// <summary>
    ///     The informational version of the assembly, as the banner prints it and as --version reports it.
    /// </summary>
    public static string Version { get; } = typeof(RunCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    private static int RejectUsage(IConsole console, string message) {
        console.WriteError(message);
        console.WriteError(UsageHint);

        return ExitCode.UsageError;
    }

    private static int ToExitCode(RuntimeOutcome outcome) {
        return outcome switch {
            RuntimeOutcome.Stopped      => ExitCode.Success,
            RuntimeOutcome.DeviceFailed => ExitCode.DeviceError,
            RuntimeOutcome.Faulted      => ExitCode.UnexpectedError,
            _                           => throw new InvalidEnumArgumentException(nameof(outcome), (int)outcome, typeof(RuntimeOutcome))
        };
    }

    #endregion

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings) {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(settings);

        SignalMeServices services = (SignalMeServices)context.Data!;
        IConsole         console  = services.Console;

        // Relaxed parsing leaves an unknown option or a stray argument in the remaining arguments rather
        // than failing on them: they are usage errors all the same, reported before anything else happens.
        string? unknownOption = context.Remaining.Parsed.Select(option => option.Key).FirstOrDefault();
        if (unknownOption is not null) { return RejectUsage(console, $"Unknown option: '{unknownOption}'."); }
        if (context.Remaining.Raw.Count > 0) { return RejectUsage(console, $"Unexpected argument: '{context.Remaining.Raw[0]}'."); }

        // Validated here rather than in Settings.Validate(), so that the message is signalme's own instead
        // of being wrapped in Spectre's validation error. The factory names the mode as it advertises it,
        // so "Mode:" and the status report spell it one way whatever spacing or capitals were typed.
        if (!SignalMeModeFactory.TryResolve(settings.Mode, out string? modeName) || !SignalMeModeFactory.TryCreate(modeName, console, out ISignalMeMode? mode)) {
            console.WriteError($"Unknown mode: '{settings.Mode}'.");
            console.WriteError($"Available modes: {string.Join(", ", SignalMeModeFactory.KnownModes)}");

            return ExitCode.UsageError;
        }

        console.WriteLine($"SignalMe {Version}");

        // Created before the discovery, as the spec's lifecycle has it: a Ctrl+C that lands during a slow
        // scan of the ports is honoured by the selector before any dialog starts.
        using CancellationTokenSource cancellation = ConsoleCancellation.OnCtrlC();

        IReadOnlyList<ILuxaforDevice> devices;
        try {
            devices = services.Discovery.Discover();
        } catch (Exception exception) {
            console.WriteError($"Could not reach a Luxafor device: {exception.GetType().Name}: {exception.Message}");
            console.WriteError("The device may be unplugged or already held by another application. Luxafor devices are driven through the Windows HID stack and cannot be reached on other systems.");

            return ExitCode.DeviceError;
        }
        if (devices.Count == 0) {
            console.WriteError("No Luxafor device detected.");
            console.WriteError("Check that a Luxafor device is connected to a USB port.");

            return ExitCode.DeviceError;
        }

        // A selection cut short, by Ctrl+C or because the input ended, throws: the selector has released
        // every device by then, and the command app's handler turns it into a clean "SignalMe stopped.".
        ILuxaforDevice device = await new LuxaforDeviceSelector(console, services.Delay).SelectAsync(devices, cancellation.Token).ConfigureAwait(false);

        console.WriteLine($"Mode: {modeName}");

        ISessionMonitor sessionMonitor = services.SessionMonitorFactory();
        SignalMeRuntime runtime        = new(modeName, mode, device, sessionMonitor, services.Store, console, services.Delay);

        RuntimeOutcome outcome = await runtime.RunAsync(cancellation.Token).ConfigureAwait(false);

        return ToExitCode(outcome);
    }

    #region Nested types declarations

    public sealed class Settings : CommandSettings {

        [CommandOption("-m|--mode <MODE>")]
        [Description("The mode SignalMe runs in, which decides where the status comes from.")]
        [DefaultValue("manual")]
        public string Mode { get; init; } = "manual";

    }

    #endregion

}
