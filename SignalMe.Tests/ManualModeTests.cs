using SignalMe.Converters;
using SignalMe.Infrastructure;
using SignalMe.Modes;
using SignalMe.Modes.Manual;
using SignalMe.Runtime;
using SignalMe.Services;
using SignalMe.Sessions;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     The manual mode over a scripted console and a recording context: what it prints when it starts,
///     which intent it sends for what is typed, what it answers by itself, and how it ends.
/// </summary>
public sealed class ManualModeTests {

    private static readonly string NewLine = Environment.NewLine;

    private static readonly string[] Hints = ["Commands: help", "Press Ctrl+C to stop.", ""];

    /// <summary>The block of spec §16, as the user must see it.</summary>
    private static readonly string HelpText = string.Join(NewLine, [
        "Statuses:",
        "  available",
        "  free",
        "  busy",
        "  do-not-disturb",
        "  dnd",
        "",
        "Signals:",
        "  happy",
        "  bored",
        "  desperate",
        "  warning",
        "  alerting",
        "  ready",
        "",
        "Commands:",
        "  status",
        "  off",
        "  help",
        "",
        "Press Ctrl+C to stop SignalMe."
    ]);

    /// <summary>Derived from the enum, so adding an outcome cannot silently leave it untested.</summary>
    public static TheoryData<IntentOutcome> AllOutcomes => new(Enum.GetValues<IntentOutcome>());

    public static TheoryData<UserStatus?, SessionState, EffectiveStatus, string> StatusReports => new() {
        { UserStatus.Busy, SessionState.Active, EffectiveStatus.Busy, "Desired status: busy" + NewLine + "Effective status: busy" + NewLine + "Session: active" },
        { UserStatus.Busy, SessionState.Locked, EffectiveStatus.Away, "Desired status: busy" + NewLine + "Effective status: away" + NewLine + "Session: locked" },
        { UserStatus.Available, SessionState.Active, EffectiveStatus.Available, "Desired status: available" + NewLine + "Effective status: available" + NewLine + "Session: active" },
        { UserStatus.DoNotDisturb, SessionState.Active, EffectiveStatus.DoNotDisturb, "Desired status: do-not-disturb" + NewLine + "Effective status: do-not-disturb" + NewLine + "Session: active" },
        { null, SessionState.Active, EffectiveStatus.Off, "Desired status: none" + NewLine + "Effective status: off" + NewLine + "Session: active" },
        { null, SessionState.Locked, EffectiveStatus.Off, "Desired status: none" + NewLine + "Effective status: off" + NewLine + "Session: locked" }
    };

    private static Task RunAsync(FakeConsole console, FakeSignalMeContext context) {
        return new ManualMode(console).RunAsync(context, CancellationToken.None);
    }

    #region start-up

    [Fact]
    public async Task The_hints_are_printed_before_the_first_prompt() {
        FakeConsole console = new();

        await RunAsync(console, new FakeSignalMeContext());

        Assert.Equal(["line: Commands: help", "line: Press Ctrl+C to stop.", "line: ", "prompt: > ", "read: <end of input>"], console.Transcript);
    }

    #endregion

    #region intents

    [Theory]
    [InlineData("available", UserStatus.Available)]
    [InlineData("free", UserStatus.Available)]
    [InlineData("busy", UserStatus.Busy)]
    [InlineData("do-not-disturb", UserStatus.DoNotDisturb)]
    [InlineData("dnd", UserStatus.DoNotDisturb)]
    public async Task A_status_is_sent_as_the_desired_status(string line, UserStatus expected) {
        FakeConsole         console = new(line);
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Assert.Equal([new SignalMeIntent.SetDesiredStatus(expected)], context.Intents);
    }

    [Theory]
    [InlineData("happy", UserMood.Happy)]
    [InlineData("bored", UserMood.Bored)]
    [InlineData("desperate", UserMood.Desperate)]
    [InlineData("warning", UserMood.Warning)]
    [InlineData("alerting", UserMood.Alerting)]
    [InlineData("ready", UserMood.Ready)]
    public async Task A_mood_is_sent_as_a_signal(string line, UserMood expected) {
        FakeConsole         console = new(line);
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Assert.Equal([new SignalMeIntent.PlaySignal(expected)], context.Intents);
    }

    [Fact]
    public async Task Off_is_sent_as_a_turn_off() {
        FakeConsole         console = new("off");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Assert.Equal([new SignalMeIntent.TurnOff()], context.Intents);
    }

    [Fact]
    public async Task Intents_are_sent_in_the_order_they_were_typed() {
        FakeConsole         console = new("busy", "happy", "off", " DND ");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        SignalMeIntent[] expected = [new SignalMeIntent.SetDesiredStatus(UserStatus.Busy), new SignalMeIntent.PlaySignal(UserMood.Happy), new SignalMeIntent.TurnOff(), new SignalMeIntent.SetDesiredStatus(UserStatus.DoNotDisturb)];
        Assert.Equal(expected, context.Intents);
    }

    /// <summary>
    ///     Whatever the coordinator answers, it already said so on the console; the mode adds nothing.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllOutcomes))]
    public async Task The_mode_prints_nothing_for_an_outcome(IntentOutcome outcome) {
        FakeConsole         console = new("busy", "happy", "off");
        FakeSignalMeContext context = new() { Outcome = _ => outcome };

        await RunAsync(console, context);

        Assert.Equal(Hints, console.Output);
        Assert.Empty(console.Error);
    }

    /// <summary>
    ///     The next prompt waits for the coordinator's answer, so that it lands under the command it answers
    ///     and a signal plays to its end before the next line is read.
    /// </summary>
    [Fact]
    public async Task An_intent_is_awaited_before_the_next_prompt() {
        FakeConsole         console = new("busy", "happy");
        FakeSignalMeContext context = new() { HoldsIntents = true };

        Task run = RunAsync(console, context);
        await context.IntentReceived;

        Assert.Equal(["> "], console.Prompts);
        Assert.Equal(1, console.Reads);

        context.Release();
        await run;

        Assert.Equal(["> ", "> ", "> "], console.Prompts);
        Assert.Equal(2, context.Intents.Count);
    }

    [Fact]
    public async Task An_empty_line_is_ignored() {
        FakeConsole         console = new("", "   ", "busy");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Assert.Equal([new SignalMeIntent.SetDesiredStatus(UserStatus.Busy)], context.Intents);
        // One prompt per line, the last one answered by the end of input; nothing said about the blank ones.
        Assert.Equal(["> ", "> ", "> ", "> "], console.Prompts);
        Assert.Equal(Hints, console.Output);
    }

    #endregion

    #region status and help

    [Theory]
    [MemberData(nameof(StatusReports))]
    public async Task Status_reports_the_mode_the_statuses_and_the_session(UserStatus? desired, SessionState session, EffectiveStatus effective, string expected) {
        FakeConsole         console = new("status");
        FakeSignalMeContext context = new() { State = new SignalMeState(desired, session, effective, null) };

        await RunAsync(console, context);

        Assert.Contains("Mode: manual" + NewLine + expected, console.Output);
        Assert.Empty(context.Intents);
    }

    [Fact]
    public async Task Status_names_the_mode_the_context_runs() {
        FakeConsole         console = new("status");
        FakeSignalMeContext context = new() { ModeName = "other" };

        await RunAsync(console, context);

        Assert.Contains(console.Output, block => block.StartsWith("Mode: other" + NewLine, StringComparison.Ordinal));
    }

    /// <summary>
    ///     A playing signal is not what "status" reports: the desired and effective statuses stay those the
    ///     signal runs over, which is what the device comes back to.
    /// </summary>
    [Fact]
    public async Task Status_reports_the_statuses_a_signal_runs_over() {
        FakeConsole         console = new("status");
        FakeSignalMeContext context = new() { State = new SignalMeState(UserStatus.Busy, SessionState.Active, EffectiveStatus.Busy, UserMood.Happy) };

        await RunAsync(console, context);

        Assert.Contains("Mode: manual" + NewLine + "Desired status: busy" + NewLine + "Effective status: busy" + NewLine + "Session: active", console.Output);
    }

    [Fact]
    public async Task Help_prints_the_commands_of_the_manual_mode() {
        FakeConsole         console = new("help");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Assert.Contains(HelpText, console.Output);
        Assert.Empty(context.Intents);
    }

    /// <summary>
    ///     The help is written out, in the spec's order; this keeps it honest against the converters, which
    ///     are what the parser accepts, so a value added to one of them cannot be forgotten here.
    /// </summary>
    [Fact]
    public async Task Help_advertises_exactly_what_the_parser_accepts() {
        FakeConsole console = new("help");

        await RunAsync(console, new FakeSignalMeContext());

        string                                      help     = Assert.Single(console.Output, block => block.StartsWith("Statuses:", StringComparison.Ordinal));
        List<(string Header, List<string> Words)> sections = new();
        foreach (string line in help.Split(NewLine)) {
            if (line.EndsWith(':')) {
                sections.Add((line, new List<string>()));
            } else if (line.StartsWith("  ", StringComparison.Ordinal)) {
                Assert.NotEmpty(sections);
                sections[^1].Words.Add(line.Trim());
            }
        }

        Assert.Equal(["Statuses:", "Signals:", "Commands:"], sections.Select(section => section.Header));
        Assert.Equal(UserStatusConverter.KnownValues, sections[0].Words);
        Assert.Equal(UserMoodConverter.KnownValues.Order(), sections[1].Words.Order());
        Assert.Equal(["status", "off", "help"], sections[2].Words);
        Assert.All(sections.SelectMany(section => section.Words), word => Assert.IsNotType<ManualCommand.Unknown>(ManualCommandParser.Parse(word)));
    }

    #endregion

    #region unknown commands

    [Fact]
    public async Task An_unknown_command_is_reported_and_the_mode_goes_on() {
        FakeConsole         console = new("buzy", "busy");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Assert.Equal([.. Hints, "Unknown command: 'buzy'." + NewLine + "Type 'help' to list available commands."], console.Output);
        Assert.Equal([new SignalMeIntent.SetDesiredStatus(UserStatus.Busy)], context.Intents);
        Assert.Equal(["> ", "> ", "> "], console.Prompts);
    }

    [Fact]
    public async Task An_unknown_command_is_quoted_as_typed_without_its_surrounding_spaces() {
        FakeConsole console = new("  Buzy now  ");

        await RunAsync(console, new FakeSignalMeContext());

        Assert.Contains("Unknown command: 'Buzy now'." + NewLine + "Type 'help' to list available commands.", console.Output);
    }

    /// <summary>
    ///     Away is what SignalMe shows by itself while the session is locked, not a status the user asks for.
    /// </summary>
    [Fact]
    public async Task Away_is_an_unknown_command() {
        FakeConsole         console = new("away");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Assert.Contains("Unknown command: 'away'." + NewLine + "Type 'help' to list available commands.", console.Output);
        Assert.Empty(context.Intents);
    }

    #endregion

    #region ending

    [Fact]
    public async Task The_end_of_input_ends_the_mode_without_an_intent() {
        FakeConsole         console = new();
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Assert.Empty(context.Intents);
        Assert.Equal(1, console.Reads);
    }

    [Fact]
    public async Task Cancelling_while_waiting_for_input_ends_the_mode_without_an_intent() {
        FakeConsole                   console      = new() { EndOfInput = false };
        FakeSignalMeContext           context      = new();
        using CancellationTokenSource cancellation = new();

        Task run = new ManualMode(console).RunAsync(context, cancellation.Token);
        await console.InputAwaited.WaitAsync(TimeSpan.FromSeconds(10));
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Empty(context.Intents);
        Assert.Equal(["> "], console.Prompts);
    }

    [Fact]
    public async Task Cancelling_while_an_intent_is_pending_ends_the_mode_without_another_prompt() {
        FakeConsole                   console      = new("busy") { EndOfInput = false };
        FakeSignalMeContext           context      = new() { HoldsIntents = true };
        using CancellationTokenSource cancellation = new();

        Task run = new ManualMode(console).RunAsync(context, cancellation.Token);
        await context.IntentReceived;
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.Equal(["> "], console.Prompts);
    }

    /// <summary>
    ///     The coordinator already reported its failure; the mode neither prints it nor hides it, it lets
    ///     the runtime classify it.
    /// </summary>
    [Fact]
    public async Task A_failure_of_the_coordinator_propagates_out_of_the_mode() {
        FakeConsole         console = new("busy", "happy");
        FakeSignalMeContext context = new() { Failure = new DeviceCommandFailedException("The Luxafor device refused to display the 'busy' status.") };

        DeviceCommandFailedException thrown = await Assert.ThrowsAsync<DeviceCommandFailedException>(() => RunAsync(console, context));

        Assert.Same(context.Failure, thrown);
        Assert.Equal([new SignalMeIntent.SetDesiredStatus(UserStatus.Busy)], context.Intents);
        Assert.Equal(1, console.Reads);
        Assert.Equal(Hints, console.Output);
        Assert.Empty(console.Error);
    }

    [Fact]
    public void A_mode_without_a_console_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ManualMode(null!));
    }

    [Fact]
    public async Task A_run_without_a_context_is_refused() {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new ManualMode(new FakeConsole()).RunAsync(null!, CancellationToken.None));
    }

    #endregion

    #region factory

    [Theory]
    [InlineData("manual")]
    [InlineData("MANUAL")]
    [InlineData(" Manual ")]
    public void The_factory_creates_the_manual_mode_whatever_the_spelling(string name) {
        Assert.True(SignalMeModeFactory.TryCreate(name, new FakeConsole(), out ISignalMeMode? mode));
        Assert.IsType<ManualMode>(mode);
    }

    [Fact]
    public void Manual_is_the_one_known_mode() {
        Assert.Equal(["manual"], SignalMeModeFactory.KnownModes);
    }

    /// <summary>
    ///     The usage message advertises the known modes, so every one of them must be creatable, whatever
    ///     spelling of it the user types.
    /// </summary>
    [Fact]
    public void Every_known_mode_can_be_created() {
        foreach (string name in SignalMeModeFactory.KnownModes) {
            Assert.True(SignalMeModeFactory.TryCreate(name, new FakeConsole(), out _), $"'{name}' is advertised but cannot be created.");
            Assert.True(SignalMeModeFactory.TryCreate($" {name.ToUpperInvariant()} ", new FakeConsole(), out _), $"'{name}' is not created when typed in capitals with spaces around.");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("teams")]
    [InlineData("manual mode")]
    public void An_unknown_name_creates_nothing(string name) {
        Assert.False(SignalMeModeFactory.TryCreate(name, new FakeConsole(), out ISignalMeMode? mode));
        Assert.Null(mode);
    }

    [Fact]
    public async Task The_created_mode_talks_through_the_given_console() {
        FakeConsole console = new();
        Assert.True(SignalMeModeFactory.TryCreate("manual", console, out ISignalMeMode? mode));

        await mode.RunAsync(new FakeSignalMeContext(), CancellationToken.None);

        Assert.Equal(Hints, console.Output);
    }

    #endregion

}
