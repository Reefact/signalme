using SignalMe.Commands;
using SignalMe.Converters;
using SignalMe.Services;
using SignalMe.Tests.Fakes;

namespace SignalMe.Tests;

/// <summary>
///     Exercises the real command line, for the paths that need neither a device nor the user's status file.
/// </summary>
public sealed class CommandLineTests {

    private static (int ExitCode, string Error) Run(params string[] args) {
        int exitCode = 0;
        (string error, Exception? thrown) = StandardError.Capture(() => exitCode = SignalMeCommandApp.Create().Run(args));

        Assert.Null(thrown);

        return (exitCode, error);
    }

    [Fact]
    public void An_unknown_status_is_a_usage_error_and_lists_what_is_accepted() {
        (int exitCode, string error) = Run("as", "buys");

        Assert.Equal(ExitCode.UsageError, exitCode);
        Assert.Contains("Unknown status or mood: 'buys'.", error);
        Assert.All(UserStatusConverter.KnownValues, value => Assert.Contains(value, error));
        Assert.All(UserMoodConverter.KnownValues, value => Assert.Contains(value, error));
    }

    [Fact]
    public void An_unknown_status_never_reaches_the_hardware() {
        // If it did, this would fail with a device error on a machine with no Luxafor plugged in, and
        // would light a LED on one that has.
        (int exitCode, _) = Run("as", "buys");

        Assert.Equal(ExitCode.UsageError, exitCode);
    }

    [Fact]
    public void The_help_lists_every_command() {
        (int exitCode, _) = Run("--help");

        Assert.Equal(ExitCode.Success, exitCode);
    }

    [Fact]
    public void Exit_codes_are_all_distinct() {
        int[] codes = [ExitCode.Success, ExitCode.UsageError, ExitCode.DeviceError, ExitCode.UnexpectedError, ExitCode.Cancelled];

        Assert.Equal(codes.Length, codes.Distinct().Count());
        Assert.Equal(0, ExitCode.Success);
    }

    /// <summary>
    ///     The argument help is written by hand; this keeps it from drifting away from what is parsed.
    /// </summary>
    [Fact]
    public void The_argument_help_mentions_every_accepted_value() {
        string description = typeof(AsCommand.Settings)
                             .GetProperty(nameof(AsCommand.Settings.Status))!
                             .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), inherit: false)
                             .Cast<System.ComponentModel.DescriptionAttribute>()
                             .Single().Description;

        Assert.All(UserStatusConverter.KnownValues, value => Assert.Contains(value, description));
        Assert.All(UserMoodConverter.KnownValues, value => Assert.Contains(value, description));
        Assert.Contains("ready", description);
        Assert.Contains("available", description);
    }

    [Theory]
    [InlineData(UserStatus.Available, "available")]
    [InlineData(UserStatus.Busy, "busy")]
    [InlineData(UserStatus.Away, "away")]
    [InlineData(UserStatus.DoNotDisturb, "do-not-disturb")]
    public void Status_prints_the_canonical_value_not_the_alias(UserStatus status, string expected) {
        Assert.Equal(expected, StatusCommand.Describe(status));
    }

    [Fact]
    public void Status_says_so_plainly_when_nothing_is_set() {
        Assert.Equal("No durable status is currently set.", StatusCommand.Describe(null));
    }

}
