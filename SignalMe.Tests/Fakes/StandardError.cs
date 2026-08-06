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

}
