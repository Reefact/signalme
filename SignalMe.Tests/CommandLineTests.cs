using Reefact.LuxaforLightingDeviceController;

using SignalMe.Commands;
using SignalMe.Infrastructure;
using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     The real command line, from the arguments to the exit code, over a scripted console and fake
///     devices: what is rejected before a device is touched, what happens without one, and whole runs
///     through the manual mode.
/// </summary>
/// <remarks>
///     The help and the version are rendered by Spectre through its own console, not through
///     <see cref="IConsole" />, and it prints nothing when the output is redirected on Linux: only their
///     exit codes are checked here, their text is checked by the tool-install script on the Windows agent.
/// </remarks>
public sealed class CommandLineTests {

    private const string FirstId   = @"\\?\hid#vid_04d8&pid_f372#7&1";
    private const string SecondId  = @"\\?\hid#vid_04d8&pid_f372#7&2";
    private const string UsageHint = "Type 'signalme --help' for usage.";

    private static readonly string NewLine = Environment.NewLine;
    private static readonly string Banner  = $"SignalMe {RunCommand.Version}";

    /// <summary>Twelve white frames then the LEDs off: what one identification wave leaves in a device's record.</summary>
    private static readonly string[] Wave = [.. Enumerable.Range(0, 12).Select(frame => $"Send(Set LED n° {frame % 6 + 1} color to #FFFFFF)"), "TurnOff"];

    #region help and version

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public async Task The_help_exits_successfully_without_looking_for_a_device(string option) {
        using Harness signalme = new(new FakeConsole());

        int exitCode = await signalme.RunAsync(option);

        Check.That(exitCode).IsEqualTo(ExitCode.Success);
        Check.That(signalme.Discovery.Calls).IsEqualTo(0);
        Check.That(signalme.Console.Reads).IsEqualTo(0);
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-v")]
    public async Task The_version_exits_successfully_without_looking_for_a_device(string option) {
        using Harness signalme = new(new FakeConsole());

        int exitCode = await signalme.RunAsync(option);

        Check.That(exitCode).IsEqualTo(ExitCode.Success);
        Check.That(signalme.Discovery.Calls).IsEqualTo(0);
        Check.That(signalme.Console.Reads).IsEqualTo(0);
    }

    [Fact]
    public void The_version_is_the_informational_version_of_the_assembly() {
        Check.That(RunCommand.Version).StartsWith("2.0.0");
    }

    #endregion

    #region usage errors

    [Theory]
    [InlineData("--mode")]
    [InlineData("-m")]
    public async Task An_unknown_mode_is_a_usage_error_before_any_device_is_touched(string option) {
        using Harness signalme = new(new FakeConsole("busy"), new FakeLuxaforDevice());

        int exitCode = await signalme.RunAsync(option, "foo");

        Check.That(exitCode).IsEqualTo(ExitCode.UsageError);
        Check.That(signalme.Console.Error).ContainsExactly(["Unknown mode: 'foo'.", "Available modes: manual"]);
        Check.That(signalme.Console.Output).IsEmpty();
        Check.That(signalme.Discovery.Calls).IsEqualTo(0);
        Check.That(signalme.Console.Reads).IsEqualTo(0);
    }

    [Fact]
    public async Task An_unknown_option_is_a_usage_error() {
        using Harness signalme = new(new FakeConsole(), new FakeLuxaforDevice());

        int exitCode = await signalme.RunAsync("--bogus");

        Check.That(exitCode).IsEqualTo(ExitCode.UsageError);
        Check.That(signalme.Console.Error).ContainsExactly(["Unknown option: '--bogus'.", UsageHint]);
        Check.That(signalme.Console.Output).IsEmpty();
        Check.That(signalme.Discovery.Calls).IsEqualTo(0);
    }

    [Fact]
    public async Task An_argument_after_the_separator_is_a_usage_error() {
        using Harness signalme = new(new FakeConsole(), new FakeLuxaforDevice());

        int exitCode = await signalme.RunAsync("--", "foo");

        Check.That(exitCode).IsEqualTo(ExitCode.UsageError);
        Check.That(signalme.Console.Error).ContainsExactly(["Unexpected argument: 'foo'.", UsageHint]);
        Check.That(signalme.Discovery.Calls).IsEqualTo(0);
    }

    [Theory]
    [InlineData("--mode")]
    [InlineData("--mode=")]
    public async Task A_mode_without_a_value_is_a_usage_error(string argument) {
        using Harness signalme = new(new FakeConsole(), new FakeLuxaforDevice());

        int exitCode = await signalme.RunAsync(argument);

        Check.That(exitCode).IsEqualTo(ExitCode.UsageError);
        Check.That(signalme.Console.Error.Count).IsEqualTo(2);
        Check.That(signalme.Console.Error[^1]).IsEqualTo(UsageHint);
        Check.That(signalme.Discovery.Calls).IsEqualTo(0);
    }

    /// <summary>
    ///     A bare argument is a parse error worded by Spectre, which cannot tell it from the name of a
    ///     sub-command signalme does not have. The docs quote that text, so it is pinned here: a Spectre
    ///     upgrade that rewords it fails this test rather than silently breaking the docs.
    /// </summary>
    [Fact]
    public async Task A_stray_argument_is_a_usage_error() {
        using Harness signalme = new(new FakeConsole(), new FakeLuxaforDevice());

        int exitCode = await signalme.RunAsync("extra");

        Check.That(exitCode).IsEqualTo(ExitCode.UsageError);
        Check.That(signalme.Console.Error).ContainsExactly(["Unknown command 'extra'.", UsageHint]);
        Check.That(signalme.Discovery.Calls).IsEqualTo(0);
    }

    #endregion

    #region device errors

    /// <summary>
    ///     The CI agent has no device and no input: signalme must stop on the device error, never on a read.
    /// </summary>
    [Fact]
    public async Task No_device_is_a_device_error_and_nothing_is_read() {
        using Harness signalme = new(new FakeConsole("busy"));

        int exitCode = await signalme.RunAsync();

        Check.That(exitCode).IsEqualTo(ExitCode.DeviceError);
        Check.That(signalme.Console.Error).ContainsExactly(["No Luxafor device detected.", "Check that a Luxafor device is connected to a USB port."]);
        Check.That(signalme.Console.Output).ContainsExactly([Banner]);
        Check.That(signalme.Discovery.Calls).IsEqualTo(1);
        Check.That(signalme.Console.Reads).IsEqualTo(0);
    }

    [Fact]
    public async Task A_discovery_that_fails_is_a_device_error() {
        using Harness signalme = new(new FakeConsole("busy"));
        signalme.Discovery.Failure = new InvalidOperationException("HID unavailable");

        int exitCode = await signalme.RunAsync();

        Check.That(exitCode).IsEqualTo(ExitCode.DeviceError);
        Check.That(signalme.Console.Error).ContainsExactly([
            "Could not reach a Luxafor device: InvalidOperationException: HID unavailable",
            "The device may be unplugged or already held by another application. Luxafor devices are driven through the Windows HID stack and cannot be reached on other systems."
        ]);
        Check.That(signalme.Console.Output).ContainsExactly([Banner]);
        Check.That(signalme.Console.Reads).IsEqualTo(0);
    }

    [Fact]
    public async Task A_device_refusing_the_first_render_is_a_device_error() {
        FakeLuxaforDevice device   = new() { RefuseFromCall = 1 };
        using Harness     signalme = new(new FakeConsole("busy"), device);

        int exitCode = await signalme.RunAsync();

        Check.That(exitCode).IsEqualTo(ExitCode.DeviceError);
        Check.That(signalme.Console.Output).ContainsExactly([Banner, "Luxafor device detected.", "Mode: manual", "SignalMe stopped."]);
        Check.That(signalme.Console.Error).ContainsExactly(["The Luxafor device refused to turn its LEDs off.", "The Luxafor device refused to turn its LEDs off."]);
        Check.That(signalme.Console.Reads).IsEqualTo(0);
        Check.That(device.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_device_unplugged_during_the_run_is_a_device_error() {
        FakeLuxaforDevice device   = new();
        using Harness     signalme = new(new FakeConsole("busy") { EndOfInput = false }, device);

        Task<int> run = signalme.RunAsync();
        // The status is shown and SignalMe waits for the next command when the device goes missing.
        await signalme.Console.InputAwaited.WaitAsync(TimeSpan.FromSeconds(10));
        signalme.DeviceMonitor.Disconnect();
        int exitCode = await run;

        Check.That(exitCode).IsEqualTo(ExitCode.DeviceError);
        Check.That(signalme.MonitoredDevice).IsSameReferenceAs(device);
        Check.That(signalme.Console.Error).ContainsExactly(["Luxafor device disconnected."]);
        Check.That(signalme.Console.Output[^1]).IsEqualTo("SignalMe stopped.");
        Check.That(device.LastCommand).IsEqualTo("SetColor(#FFFF00)");
        Check.That(device.IsDisposed).IsTrue();
        Check.That(signalme.DeviceMonitor.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_session_monitor_that_cannot_start_is_an_unexpected_error() {
        FakeLuxaforDevice device   = new();
        using Harness     signalme = new(new FakeConsole("busy"), device);
        signalme.Monitor.StartFailure = new InvalidOperationException("no window");

        int exitCode = await signalme.RunAsync();

        Check.That(exitCode).IsEqualTo(ExitCode.UnexpectedError);
        Check.That(signalme.Console.Error).ContainsExactly(["signalme: InvalidOperationException: no window"]);
        Check.That(signalme.Console.Output[^1]).IsEqualTo("SignalMe stopped.");
        Check.That(device.LastCommand).IsEqualTo("TurnOff");
        Check.That(device.IsDisposed).IsTrue();
    }

    #endregion

    #region runs

    [Fact]
    public async Task A_run_with_one_device_goes_from_the_banner_to_the_device_off() {
        FakeLuxaforDevice device   = new();
        using Harness     signalme = new(new FakeConsole("busy", "happy"), device);

        int exitCode = await signalme.RunAsync();

        Check.That(exitCode).IsEqualTo(ExitCode.Success);
        string[] expected = [
            Banner,
            "Luxafor device detected.",
            "Mode: manual",
            "Status: off",
            "Commands: help",
            "Press Ctrl+C to stop.",
            "",
            "Status: busy",
            "",
            "Playing: happy",
            "Restored: busy",
            "",
            "Stopping SignalMe...",
            "SignalMe stopped."
        ];
        Check.That(signalme.Console.Output).IsEqualTo(expected);
        Check.That(signalme.Console.Error).IsEmpty();
        Check.That(device.Commands.Take(2)).ContainsExactly(["TurnOff", "SetColor(#FFFF00)"]);
        Check.That(device.LastCommand).IsEqualTo("TurnOff");
        Check.That(device.IsDisposed).IsTrue();
        Check.That(signalme.Store.Get()).IsEqualTo(UserStatus.Busy);
        Check.That(signalme.Monitor.Started).IsTrue();
        Check.That(signalme.Monitor.IsDisposed).IsTrue();
    }

    [Fact]
    public async Task A_run_with_several_devices_goes_through_the_selection_dialog() {
        FakeLuxaforDevice first    = new() { Path = FirstId };
        FakeLuxaforDevice second   = new() { Path = SecondId };
        using Harness     signalme = new(new FakeConsole("1", "n", "2", "y", "busy"), first, second);

        int exitCode = await signalme.RunAsync();

        Check.That(exitCode).IsEqualTo(ExitCode.Success);
        IReadOnlyList<string> output = signalme.Console.Output;
        Check.That(output).Contains("2 Luxafor devices detected.");
        Check.That(output).HasElementThatMatches(block => block.Contains(FirstId, StringComparison.Ordinal) && block.Contains(SecondId, StringComparison.Ordinal));
        Check.That(output).Contains("Identifying device #1...");
        Check.That(output).Contains("Identifying device #2...");
        Check.That(output).Contains($"Device selected: {SecondId}");
        Check.That(output).Contains("Mode: manual");
        Check.That(output.ToList().IndexOf($"Device selected: {SecondId}") < output.ToList().IndexOf("Mode: manual")).IsTrue();
        Check.That(output).Contains("Status: busy");
        Check.That(signalme.Console.Error).IsEmpty();

        // The declined device only ever played its wave, and was released as soon as the other was chosen.
        Check.That(first.Commands).IsEqualTo(Wave);
        Check.That(first.IsDisposed).IsTrue();

        // The chosen one played its wave, was driven through the run, and ended off and released.
        Check.That(second.Commands.Take(Wave.Length)).IsEqualTo(Wave);
        Check.That(second.Commands).Contains("SetColor(#FFFF00)");
        Check.That(second.LastCommand).IsEqualTo("TurnOff");
        Check.That(second.IsDisposed).IsTrue();
    }

    /// <summary>
    ///     The mode is named by its canonical name, not as typed: "Mode: manual" at start-up and in the
    ///     status report alike.
    /// </summary>
    [Fact]
    public async Task The_mode_is_printed_by_its_canonical_name_whatever_was_typed() {
        using Harness signalme = new(new FakeConsole("status"), new FakeLuxaforDevice());

        int exitCode = await signalme.RunAsync("--mode", " MANUAL ");

        Check.That(exitCode).IsEqualTo(ExitCode.Success);
        Check.That(signalme.Console.Output).Contains("Mode: manual");
        Check.That(signalme.Console.Output).Contains("Mode: manual" + NewLine + "Desired status: none" + NewLine + "Effective status: off" + NewLine + "Session: active");
        Check.That(signalme.Console.Output).Not.HasElementThatMatches(block => block.Contains("MANUAL", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_selection_cut_short_stops_cleanly() {
        FakeLuxaforDevice first    = new() { Path = FirstId };
        FakeLuxaforDevice second   = new() { Path = SecondId };
        using Harness     signalme = new(new FakeConsole(), first, second);

        int exitCode = await signalme.RunAsync();

        Check.That(exitCode).IsEqualTo(ExitCode.Success);
        Check.That(signalme.Console.Output[^1]).IsEqualTo("SignalMe stopped.");
        Check.That(signalme.Console.Error).IsEmpty();
        Check.That(signalme.Console.Output).Not.Contains("Mode: manual");
        Check.That(first.IsDisposed).IsTrue();
        Check.That(second.IsDisposed).IsTrue();
        Check.That(signalme.Monitor.Started).IsFalse();
    }

    #endregion

    [Fact]
    public void Exit_codes_are_all_distinct() {
        int[] codes = [ExitCode.Success, ExitCode.UsageError, ExitCode.DeviceError, ExitCode.UnexpectedError];

        Check.That(codes.Distinct().Count()).IsEqualTo(codes.Length);
        Check.That(ExitCode.Success).IsEqualTo(0);
    }

    /// <summary>
    ///     The real command line over fakes: the console is scripted, the discovery finds the given devices,
    ///     the status store is a throwaway folder and the session monitor is driven by hand.
    /// </summary>
    private sealed class Harness : IDisposable {

        private readonly TemporaryStatusStore _store = new();

        public Harness(FakeConsole console, params ILuxaforDevice[] devices) {
            Console   = console;
            Discovery = new FakeDiscovery(devices);
        }

        public FakeConsole                 Console       { get; }
        public FakeDiscovery               Discovery     { get; }
        public FakeSessionMonitor          Monitor       { get; } = new();
        public FakeDeviceConnectionMonitor DeviceMonitor { get; } = new();

        /// <summary>The device the command asked a connection monitor for: the one selected, or none yet.</summary>
        public ILuxaforDevice? MonitoredDevice { get; private set; }

        public UserCurrentStatus Store => _store.Store;

        public Task<int> RunAsync(params string[] args) {
            SignalMeServices services = new() {
                Console               = Console,
                Delay                 = new InstantDelay(),
                Discovery             = Discovery,
                Store                 = _store.Store,
                SessionMonitorFactory = () => Monitor,
                DeviceMonitorFactory  = device => {
                    MonitoredDevice = device;

                    return DeviceMonitor;
                }
            };

            return SignalMeCommandApp.Create(services).RunAsync(args);
        }

        public void Dispose() {
            _store.Dispose();
        }

    }

}
