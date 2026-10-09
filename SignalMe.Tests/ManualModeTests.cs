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

    /// <summary>The blank line that closes each exchange, before the next prompt: not an answer to anything.</summary>
    private const string Separator = "";

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

        Check.That(console.Transcript).ContainsExactly(["line: Commands: help", "line: Press Ctrl+C to stop.", "line: ", "prompt: > ", "read: <end of input>"]);
    }

    /// <summary>
    ///     Spec §42: a blank line closes every exchange, so that the next prompt never runs into what
    ///     answered the previous line.
    /// </summary>
    [Fact]
    public async Task A_blank_line_separates_each_exchange_from_the_next_prompt() {
        FakeConsole console = new("busy", "status");

        await RunAsync(console, new FakeSignalMeContext());

        string[] expected = [
            "line: Commands: help",
            "line: Press Ctrl+C to stop.",
            "line: ",
            "prompt: > ",
            "read: busy",
            "line: ",
            "prompt: > ",
            "read: status",
            "line: Mode: manual" + NewLine + "Desired status: none" + NewLine + "Effective status: off" + NewLine + "Session: active",
            "line: ",
            "prompt: > ",
            "read: <end of input>"
        ];
        Check.That(console.Transcript).IsEqualTo(expected);
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

        Check.That(context.Intents).ContainsExactly([new SignalMeIntent.SetDesiredStatus(expected)]);
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

        Check.That(context.Intents).ContainsExactly([new SignalMeIntent.PlaySignal(expected)]);
    }

    [Fact]
    public async Task Off_is_sent_as_a_turn_off() {
        FakeConsole         console = new("off");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Check.That(context.Intents).ContainsExactly([new SignalMeIntent.TurnOff()]);
    }

    [Fact]
    public async Task Intents_are_sent_in_the_order_they_were_typed() {
        FakeConsole         console = new("busy", "happy", "off", " DND ");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        SignalMeIntent[] expected = [new SignalMeIntent.SetDesiredStatus(UserStatus.Busy), new SignalMeIntent.PlaySignal(UserMood.Happy), new SignalMeIntent.TurnOff(), new SignalMeIntent.SetDesiredStatus(UserStatus.DoNotDisturb)];
        Check.That(context.Intents).IsEqualTo(expected);
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

        Check.That(console.Output).ContainsExactly([.. Hints, Separator, Separator, Separator]);
        Check.That(console.Error).IsEmpty();
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

        Check.That(console.Prompts).ContainsExactly(["> "]);
        Check.That(console.Reads).IsEqualTo(1);

        context.Release();
        await run;

        Check.That(console.Prompts).ContainsExactly(["> ", "> ", "> "]);
        Check.That(context.Intents.Count).IsEqualTo(2);
    }

    /// <summary>
    ///     Spec §11: an empty line is ignored. Nothing answers it, so no separator closes it either: the
    ///     prompt comes straight back rather than under a doubled gap.
    /// </summary>
    [Fact]
    public async Task An_empty_line_is_ignored() {
        FakeConsole         console = new("", "   ", "busy");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Check.That(context.Intents).ContainsExactly([new SignalMeIntent.SetDesiredStatus(UserStatus.Busy)]);
        // One prompt per line, the last one answered by the end of input; nothing said about the blank ones.
        Check.That(console.Prompts).ContainsExactly(["> ", "> ", "> ", "> "]);
        Check.That(console.Output).ContainsExactly([.. Hints, Separator]);
        Check.That(console.Transcript.Skip(Hints.Length)).ContainsExactly(["prompt: > ", "read: ", "prompt: > ", "read:    ", "prompt: > ", "read: busy", "line: ", "prompt: > ", "read: <end of input>"]);
    }

    #endregion

    #region status and help

    [Theory]
    [MemberData(nameof(StatusReports))]
    public async Task Status_reports_the_mode_the_statuses_and_the_session(UserStatus? desired, SessionState session, EffectiveStatus effective, string expected) {
        FakeConsole         console = new("status");
        FakeSignalMeContext context = new() { State = new SignalMeState(desired, session, effective, null) };

        await RunAsync(console, context);

        Check.That(console.Output).Contains("Mode: manual" + NewLine + expected);
        Check.That(context.Intents).IsEmpty();
    }

    [Fact]
    public async Task Status_names_the_mode_the_context_runs() {
        FakeConsole         console = new("status");
        FakeSignalMeContext context = new() { ModeName = "other" };

        await RunAsync(console, context);

        Check.That(console.Output).HasElementThatMatches(block => block.StartsWith("Mode: other" + NewLine, StringComparison.Ordinal));
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

        Check.That(console.Output).Contains("Mode: manual" + NewLine + "Desired status: busy" + NewLine + "Effective status: busy" + NewLine + "Session: active");
    }

    [Fact]
    public async Task Help_prints_the_commands_of_the_manual_mode() {
        FakeConsole         console = new("help");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Check.That(console.Output).Contains(HelpText);
        Check.That(context.Intents).IsEmpty();
    }

    /// <summary>
    ///     The help is written out, in the spec's order; this keeps it honest against the converters, which
    ///     are what the parser accepts, so a value added to one of them cannot be forgotten here.
    /// </summary>
    [Fact]
    public async Task Help_advertises_exactly_what_the_parser_accepts() {
        FakeConsole console = new("help");

        await RunAsync(console, new FakeSignalMeContext());

        string[] helps = console.Output.Where(block => block.StartsWith("Statuses:", StringComparison.Ordinal)).ToArray();
        Check.That(helps).HasSize(1);
        string                                    help     = helps[0];
        List<(string Header, List<string> Words)> sections = new();
        foreach (string line in help.Split(NewLine)) {
            if (line.EndsWith(':')) {
                sections.Add((line, new List<string>()));
            } else if (line.StartsWith("  ", StringComparison.Ordinal)) {
                Check.That(sections).Not.IsEmpty();
                sections[^1].Words.Add(line.Trim());
            }
        }

        Check.That(sections.Select(section => section.Header)).ContainsExactly(["Statuses:", "Signals:", "Commands:"]);
        Check.That(sections[0].Words).IsEqualTo(UserStatusConverter.KnownValues);
        Check.That(sections[1].Words.Order()).IsEqualTo(UserMoodConverter.KnownValues.Order());
        Check.That(sections[2].Words).ContainsExactly(["status", "off", "help"]);
        foreach (string word in sections.SelectMany(section => section.Words)) {
            Check.That(ManualCommandParser.Parse(word)).IsNotInstanceOf<ManualCommand.Unknown>();
        }
    }

    #endregion

    #region unknown commands

    [Fact]
    public async Task An_unknown_command_is_reported_and_the_mode_goes_on() {
        FakeConsole         console = new("buzy", "busy");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Check.That(console.Output).ContainsExactly([.. Hints, "Unknown command: 'buzy'." + NewLine + "Type 'help' to list available commands.", Separator, Separator]);
        Check.That(context.Intents).ContainsExactly([new SignalMeIntent.SetDesiredStatus(UserStatus.Busy)]);
        Check.That(console.Prompts).ContainsExactly(["> ", "> ", "> "]);
    }

    [Fact]
    public async Task An_unknown_command_is_quoted_as_typed_without_its_surrounding_spaces() {
        FakeConsole console = new("  Buzy now  ");

        await RunAsync(console, new FakeSignalMeContext());

        Check.That(console.Output).Contains("Unknown command: 'Buzy now'." + NewLine + "Type 'help' to list available commands.");
    }

    /// <summary>
    ///     Away is what SignalMe shows by itself while the session is locked, not a status the user asks for.
    /// </summary>
    [Fact]
    public async Task Away_is_an_unknown_command() {
        FakeConsole         console = new("away");
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Check.That(console.Output).Contains("Unknown command: 'away'." + NewLine + "Type 'help' to list available commands.");
        Check.That(context.Intents).IsEmpty();
    }

    #endregion

    #region ending

    [Fact]
    public async Task The_end_of_input_ends_the_mode_without_an_intent() {
        FakeConsole         console = new();
        FakeSignalMeContext context = new();

        await RunAsync(console, context);

        Check.That(context.Intents).IsEmpty();
        Check.That(console.Reads).IsEqualTo(1);
    }

    [Fact]
    public async Task Cancelling_while_waiting_for_input_ends_the_mode_without_an_intent() {
        FakeConsole                   console      = new() { EndOfInput = false };
        FakeSignalMeContext           context      = new();
        using CancellationTokenSource cancellation = new();

        Task run = new ManualMode(console).RunAsync(context, cancellation.Token);
        await console.InputAwaited.WaitAsync(TimeSpan.FromSeconds(10));
        await cancellation.CancelAsync();

        Check.ThatCode(() => run).Throws<OperationCanceledException>();
        Check.That(context.Intents).IsEmpty();
        Check.That(console.Prompts).ContainsExactly(["> "]);
    }

    [Fact]
    public async Task Cancelling_while_an_intent_is_pending_ends_the_mode_without_another_prompt() {
        FakeConsole                   console      = new("busy") { EndOfInput = false };
        FakeSignalMeContext           context      = new() { HoldsIntents = true };
        using CancellationTokenSource cancellation = new();

        Task run = new ManualMode(console).RunAsync(context, cancellation.Token);
        await context.IntentReceived;
        await cancellation.CancelAsync();

        Check.ThatCode(() => run).Throws<OperationCanceledException>();
        Check.That(console.Prompts).ContainsExactly(["> "]);
    }

    /// <summary>
    ///     The coordinator already reported its failure; the mode neither prints it nor hides it, it lets
    ///     the runtime classify it.
    /// </summary>
    [Fact]
    public void A_failure_of_the_coordinator_propagates_out_of_the_mode() {
        FakeConsole         console = new("busy", "happy");
        FakeSignalMeContext context = new() { Failure = new DeviceCommandFailedException("The Luxafor device refused to display the 'busy' status.") };

        DeviceCommandFailedException thrown = Check.ThatCode(() => RunAsync(console, context)).Throws<DeviceCommandFailedException>().Value;

        Check.That(thrown).IsSameReferenceAs(context.Failure);
        Check.That(context.Intents).ContainsExactly([new SignalMeIntent.SetDesiredStatus(UserStatus.Busy)]);
        Check.That(console.Reads).IsEqualTo(1);
        Check.That(console.Output).IsEqualTo(Hints);
        Check.That(console.Error).IsEmpty();
    }

    [Fact]
    public void A_mode_without_a_console_is_refused() {
        Check.ThatCode(() => new ManualMode(null!)).Throws<ArgumentNullException>();
    }

    [Fact]
    public void A_run_without_a_context_is_refused() {
        Check.ThatCode(() => new ManualMode(new FakeConsole()).RunAsync(null!, CancellationToken.None)).Throws<ArgumentNullException>();
    }

    #endregion

    #region factory

    [Theory]
    [InlineData("manual")]
    [InlineData("MANUAL")]
    [InlineData(" Manual ")]
    public void The_factory_creates_the_manual_mode_whatever_the_spelling(string name) {
        Check.That(SignalMeModeFactory.TryCreate(name, new FakeConsole(), out ISignalMeMode? mode)).IsTrue();
        Check.That(mode).IsInstanceOf<ManualMode>();
    }

    /// <summary>
    ///     "Mode: manual" and the status report spell the mode the way the factory advertises it, whatever
    ///     spacing or capitals the user typed on the command line.
    /// </summary>
    [Theory]
    [InlineData("manual")]
    [InlineData("MANUAL")]
    [InlineData(" Manual ")]
    public void The_factory_resolves_the_advertised_name_whatever_the_spelling(string name) {
        Check.That(SignalMeModeFactory.TryResolve(name, out string? canonicalName)).IsTrue();
        Check.That(canonicalName).IsEqualTo("manual");
    }

    [Fact]
    public void Manual_is_the_one_known_mode() {
        Check.That(SignalMeModeFactory.KnownModes).ContainsExactly(["manual"]);
    }

    /// <summary>
    ///     The usage message advertises the known modes, so every one of them must be creatable, whatever
    ///     spelling of it the user types; and whatever the factory creates, it must also name, since the
    ///     command line prints the name it resolved and then creates the mode from it.
    /// </summary>
    [Fact]
    public void Every_known_mode_can_be_created_and_resolved() {
        foreach (string name in SignalMeModeFactory.KnownModes) {
            string spelling = $" {name.ToUpperInvariant()} ";

            Check.WithCustomMessage($"'{name}' is advertised but cannot be created.").That(SignalMeModeFactory.TryCreate(name, new FakeConsole(), out _)).IsTrue();
            Check.WithCustomMessage($"'{name}' is not created when typed in capitals with spaces around.").That(SignalMeModeFactory.TryCreate(spelling, new FakeConsole(), out _)).IsTrue();
            Check.WithCustomMessage($"'{name}' is not resolved when typed in capitals with spaces around.").That(SignalMeModeFactory.TryResolve(spelling, out string? canonicalName)).IsTrue();
            Check.That(canonicalName).IsEqualTo(name);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("teams")]
    [InlineData("manual mode")]
    public void An_unknown_name_creates_nothing(string name) {
        Check.That(SignalMeModeFactory.TryCreate(name, new FakeConsole(), out ISignalMeMode? mode)).IsFalse();
        Check.That(mode).IsNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("teams")]
    [InlineData("manual mode")]
    public void An_unknown_name_resolves_to_nothing(string name) {
        Check.That(SignalMeModeFactory.TryResolve(name, out string? canonicalName)).IsFalse();
        Check.That(canonicalName).IsNull();
    }

    [Fact]
    public async Task The_created_mode_talks_through_the_given_console() {
        FakeConsole console = new();
        Check.That(SignalMeModeFactory.TryCreate("manual", console, out ISignalMeMode? mode)).IsTrue();

        // NFluent's checks carry no nullability annotation: the compiler cannot tell that TryCreate succeeded.
        await mode!.RunAsync(new FakeSignalMeContext(), CancellationToken.None);

        Check.That(console.Output).IsEqualTo(Hints);
    }

    #endregion

}
