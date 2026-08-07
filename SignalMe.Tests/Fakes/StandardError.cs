namespace SignalMe.Tests.Fakes;

internal static class StandardError {

    /// <summary>
    ///     Runs <paramref name="action" /> and returns both what it wrote on the error output and the
    ///     exception it threw, if any.
    /// </summary>
    public static (string Output, Exception? Thrown) Capture(Action action) {
        TextWriter   original = Console.Error;
        StringWriter captured = new();
        Console.SetError(captured);
        try {
            action();

            return (captured.ToString(), null);
        } catch (Exception exception) {
            return (captured.ToString(), exception);
        } finally {
            Console.SetError(original);
        }
    }

    /// <summary>
    ///     Returns what <paramref name="action" /> wrote on the error output. Exceptions are left to the
    ///     caller, which usually captures them with <c>Record.ExceptionAsync</c>.
    /// </summary>
    public static async Task<string> CaptureAsync(Func<Task> action) {
        TextWriter   original = Console.Error;
        StringWriter captured = new();
        Console.SetError(captured);
        try {
            await action();

            return captured.ToString();
        } finally {
            Console.SetError(original);
        }
    }

}
