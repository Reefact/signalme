using SignalMe.Commands;
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
    public void The_help_exits_successfully() {
        (int exitCode, _) = Run("--help");

        Assert.Equal(ExitCode.Success, exitCode);
    }

    [Fact]
    public void Exit_codes_are_all_distinct() {
        int[] codes = [ExitCode.Success, ExitCode.UsageError, ExitCode.DeviceError, ExitCode.UnexpectedError];

        Assert.Equal(codes.Length, codes.Distinct().Count());
        Assert.Equal(0, ExitCode.Success);
    }

}
