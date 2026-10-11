using SignalMe.Commands;

namespace SignalMe.Tests;

/// <summary>
///     How the arguments are parsed, as a pure function: which request each command line produces. What
///     each request then prints, and the exit code it returns, is covered by <see cref="CommandLineTests" />.
/// </summary>
public sealed class CommandLineParserTests {

    public static TheoryData<string[], string> RunRequests => new() {
        { [], "manual" },
        { ["--mode", "manual"], "manual" },
        { ["-m", "manual"], "manual" },
        { ["--mode=manual"], "manual" },
        { ["-m=manual"], "manual" },
        // Passed on as typed: the mode factory trims it and ignores its case, and names an unknown one.
        { ["--mode", " MANUAL "], " MANUAL " },
        { ["--mode", "foo"], "foo" },
        // A repeated option keeps its last value.
        { ["--mode", "foo", "--mode", "manual"], "manual" },
        // A separator with nothing after it is harmless.
        { ["--"], "manual" }
    };

    public static TheoryData<string[], string> InvalidRequests => new() {
        { ["--bogus"], "Unknown option: '--bogus'." },
        { ["--bogus=1"], "Unknown option: '--bogus'." },
        { ["-x"], "Unknown option: '-x'." },
        { ["extra"], "Unexpected argument: 'extra'." },
        { ["--", "--mode"], "Unexpected argument: '--mode'." },
        { ["--mode"], "Option '--mode' needs a value: --mode <MODE>." },
        { ["-m"], "Option '-m' needs a value: -m <MODE>." },
        { ["--mode="], "Option '--mode' needs a value: --mode <MODE>." },
        { ["-m="], "Option '-m' needs a value: -m <MODE>." },
        { ["--mode", ""], "Option '--mode' needs a value: --mode <MODE>." },
        // A mode name never starts with a dash, so a value that does is treated as missing, whichever way
        // it is attached to the option.
        { ["--mode", "--bogus"], "Option '--mode' needs a value: --mode <MODE>." },
        { ["--mode", "-"], "Option '--mode' needs a value: --mode <MODE>." },
        { ["--mode=-bogus"], "Option '--mode' needs a value: --mode <MODE>." },
        { ["-m=-bogus"], "Option '-m' needs a value: -m <MODE>." },
        { ["--mode=-"], "Option '--mode' needs a value: --mode <MODE>." },
        // Only the first problem encountered is reported.
        { ["extra", "--bogus"], "Unexpected argument: 'extra'." }
    };

    [Theory]
    [MemberData(nameof(RunRequests))]
    public void A_line_that_runs_signalme_carries_the_mode_as_typed(string[] args, string mode) {
        Check.That(CommandLineParser.Parse(args)).IsEqualTo(new CommandLineRequest.Run(mode));
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void A_usage_error_names_what_was_wrong(string[] args, string reason) {
        Check.That(CommandLineParser.Parse(args)).IsEqualTo(new CommandLineRequest.Invalid(reason));
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void The_help_is_asked_by_either_spelling(string option) {
        Check.That(CommandLineParser.Parse([option])).IsInstanceOf<CommandLineRequest.ShowHelp>();
    }

    [Theory]
    [InlineData("--version")]
    [InlineData("-v")]
    public void The_version_is_asked_by_either_spelling(string option) {
        Check.That(CommandLineParser.Parse([option])).IsInstanceOf<CommandLineRequest.ShowVersion>();
    }

    /// <summary>
    ///     Help and version have no side effects, and someone asking for help with a command line they got
    ///     wrong should get it: both take precedence over a usage error, and help takes precedence over
    ///     version.
    /// </summary>
    [Fact]
    public void Help_and_version_take_precedence_over_the_rest_of_the_command_line() {
        Check.That(CommandLineParser.Parse(["--bogus", "--help"])).IsInstanceOf<CommandLineRequest.ShowHelp>();
        Check.That(CommandLineParser.Parse(["extra", "-v"])).IsInstanceOf<CommandLineRequest.ShowVersion>();
        Check.That(CommandLineParser.Parse(["--version", "--help"])).IsInstanceOf<CommandLineRequest.ShowHelp>();
    }

    [Fact]
    public void A_help_placed_after_the_separator_is_an_argument_not_a_request() {
        Check.That(CommandLineParser.Parse(["--", "--help"])).IsEqualTo(new CommandLineRequest.Invalid("Unexpected argument: '--help'."));
    }

}
