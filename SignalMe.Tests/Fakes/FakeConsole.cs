using SignalMe.Infrastructure;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     A console whose input is scripted and whose output is captured.
/// </summary>
/// <remarks>
///     The coordinator writes from its loop while the mode reads and writes from its own task, so every
///     list is guarded by one lock, and <see cref="Transcript" /> keeps reads and writes in the order they
///     happened: that is what lets a test check that the initial status was printed before the mode's
///     first prompt.
/// </remarks>
public sealed class FakeConsole : IConsole {

    private readonly object        _lock = new();
    private readonly Queue<string> _lines;
    private readonly List<string>  _output     = new();
    private readonly List<string>  _prompts    = new();
    private readonly List<string>  _error      = new();
    private readonly List<string>  _transcript = new();

    private readonly List<(int Count, TaskCompletionSource Waiter)> _outputWaiters = new();
    private readonly TaskCompletionSource                           _inputAwaited  = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Answers each read with the next line, then reports the end of input.</summary>
    public FakeConsole(params string[] lines) {
        _lines = new Queue<string>(lines);
    }

    /// <summary>
    ///     What a read past the script does: end of input (null) by default, or a read that stays pending
    ///     until the token is cancelled, like a user who never presses Enter before Ctrl+C.
    /// </summary>
    public bool EndOfInput { get; init; } = true;

    /// <summary>The blocks written with <see cref="WriteLine" />, in order.</summary>
    public IReadOnlyList<string> Output {
        get {
            lock (_lock) { return _output.ToArray(); }
        }
    }

    /// <summary>The prompts written with <see cref="Write" />, in order.</summary>
    public IReadOnlyList<string> Prompts {
        get {
            lock (_lock) { return _prompts.ToArray(); }
        }
    }

    /// <summary>The lines written on the error output, in order.</summary>
    public IReadOnlyList<string> Error {
        get {
            lock (_lock) { return _error.ToArray(); }
        }
    }

    /// <summary>
    ///     Every interaction in order: <c>read: busy</c>, <c>read: &lt;end of input&gt;</c>, <c>read: &lt;waiting&gt;</c>,
    ///     <c>prompt: &gt; </c>, <c>line: Status: busy</c> and <c>error: ...</c>.
    /// </summary>
    public IReadOnlyList<string> Transcript {
        get {
            lock (_lock) { return _transcript.ToArray(); }
        }
    }

    /// <summary>How many reads were asked for, whatever they returned.</summary>
    public int Reads { get; private set; }

    /// <summary>Completes once a read is waiting for input nobody will type.</summary>
    public Task InputAwaited => _inputAwaited.Task;

    /// <inheritdoc />
    public Task<string?> ReadLineAsync(CancellationToken cancellationToken) {
        if (cancellationToken.IsCancellationRequested) { return Task.FromCanceled<string?>(cancellationToken); }

        lock (_lock) {
            Reads++;
            if (_lines.TryDequeue(out string? line)) {
                _transcript.Add($"read: {line}");

                return Task.FromResult<string?>(line);
            }
            if (EndOfInput) {
                _transcript.Add("read: <end of input>");

                return Task.FromResult<string?>(null);
            }

            _transcript.Add("read: <waiting>");
            _inputAwaited.TrySetResult();
        }

        return WaitForCancellationAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Write(string text) {
        lock (_lock) {
            _prompts.Add(text);
            _transcript.Add($"prompt: {text}");
        }
    }

    /// <inheritdoc />
    public void WriteLine(string text) {
        lock (_lock) {
            _output.Add(text);
            _transcript.Add($"line: {text}");

            // Woken under the lock, so a waiter can never miss the block it waits for.
            for (int i = _outputWaiters.Count - 1; i >= 0; i--) {
                if (_output.Count < _outputWaiters[i].Count) { continue; }

                _outputWaiters[i].Waiter.TrySetResult();
                _outputWaiters.RemoveAt(i);
            }
        }
    }

    /// <inheritdoc />
    public void WriteError(string line) {
        lock (_lock) {
            _error.Add(line);
            _transcript.Add($"error: {line}");
        }
    }

    /// <summary>
    ///     Completes once at least <paramref name="count" /> blocks have been written, which is how a test
    ///     waits for the coordinator to have handled a session change it raised. Bounded, so that a bug
    ///     fails the test instead of hanging it.
    /// </summary>
    public Task WaitForOutputAsync(int count) {
        TaskCompletionSource waiter = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock) {
            if (_output.Count >= count) { return Task.CompletedTask; }

            _outputWaiters.Add((count, waiter));
        }

        return waiter.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    private static async Task<string?> WaitForCancellationAsync(CancellationToken cancellationToken) {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

        return null;
    }

}
