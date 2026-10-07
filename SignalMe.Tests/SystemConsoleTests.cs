using SignalMe.Infrastructure;

namespace SignalMe.Tests;

/// <summary>
///     The real console's one piece of logic: a notification arriving while the prompt waits for the user
///     must not land on the prompt line, and the prompt must come back after it.
/// </summary>
/// <remarks>
///     Redirects the process-wide console streams, which is why the suite runs one test at a time.
/// </remarks>
public sealed class SystemConsoleTests {

    private static readonly string NewLine = Environment.NewLine;

    [Fact]
    public async Task A_block_written_while_a_prompt_is_pending_goes_on_its_own_lines_and_the_prompt_comes_back() {
        TextWriter   originalOut = Console.Out;
        TextReader   originalIn  = Console.In;
        StringWriter output      = new();
        Console.SetOut(output);
        Console.SetIn(new StringReader("busy" + NewLine));
        try {
            SystemConsole console = new();

            console.Write("> ");
            console.WriteLine("Windows session locked." + NewLine + "Effective status: away");
            string? line = await console.ReadLineAsync(CancellationToken.None);
            // The read completed, so the prompt is no longer pending: this one is written plainly.
            console.WriteLine("Status: busy");

            Assert.Equal("busy", line);
            Assert.Equal("> " + NewLine + "Windows session locked." + NewLine + "Effective status: away" + NewLine + "> " + "Status: busy" + NewLine, output.ToString());
        } finally {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
        }
    }

    [Fact]
    public void A_block_written_with_no_prompt_pending_is_written_plainly() {
        TextWriter   originalOut = Console.Out;
        StringWriter output      = new();
        Console.SetOut(output);
        try {
            new SystemConsole().WriteLine("Status: busy");

            Assert.Equal("Status: busy" + NewLine, output.ToString());
        } finally {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Ctrl+C while the prompt waits: nobody will finish that line, so the console ends it, and what is
    ///     printed next ("Stopping SignalMe...") starts on a line of its own without the prompt coming back.
    /// </summary>
    [Fact]
    public async Task A_cancelled_read_ends_the_prompt_line_and_forgets_the_prompt() {
        TextWriter     originalOut = Console.Out;
        TextReader     originalIn  = Console.In;
        StringWriter   output      = new();
        BlockingReader input       = new();
        Console.SetOut(output);
        Console.SetIn(input);
        try {
            SystemConsole                 console      = new();
            using CancellationTokenSource cancellation = new();

            console.Write("> ");
            Task<string?> read = console.ReadLineAsync(cancellation.Token);
            await cancellation.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => read);
            console.WriteLine("Stopping SignalMe...");

            Assert.Equal("> " + NewLine + "Stopping SignalMe..." + NewLine, output.ToString());
        } finally {
            // Frees the thread still blocked in Console.ReadLine, which would otherwise outlive the test.
            input.Release();
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
        }
    }

    /// <summary>
    ///     End of input while the prompt waits, as with a redirected stdin: nothing echoed an Enter, so the
    ///     console ends the line itself, as it does for a cancelled read, and the stop line starts on a line
    ///     of its own.
    /// </summary>
    [Fact]
    public async Task A_read_that_hits_the_end_of_the_input_ends_the_prompt_line_and_forgets_the_prompt() {
        TextWriter   originalOut = Console.Out;
        TextReader   originalIn  = Console.In;
        StringWriter output      = new();
        Console.SetOut(output);
        Console.SetIn(new StringReader(string.Empty));
        try {
            SystemConsole console = new();

            console.Write("> ");
            string? line = await console.ReadLineAsync(CancellationToken.None);
            console.WriteLine("Stopping SignalMe...");

            Assert.Null(line);
            Assert.Equal("> " + NewLine + "Stopping SignalMe..." + NewLine, output.ToString());
        } finally {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
        }
    }

    /// <summary>
    ///     A reader that blocks like a keyboard nobody types on, until released. Gated by a task rather than
    ///     an event: the abandoned read thread may still be inside <see cref="ReadLine" /> when the test
    ///     ends, and a task has nothing to dispose from under it.
    /// </summary>
    private sealed class BlockingReader : TextReader {

        private readonly TaskCompletionSource _gate = new();

        public void Release() {
            _gate.TrySetResult();
        }

        public override string? ReadLine() {
            // Blocking on purpose: this stands in for Console.ReadLine on the thread SystemConsole gives it.
            _gate.Task.Wait();

            return null;
        }

    }

}
