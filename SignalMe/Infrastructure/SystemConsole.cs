#region Usings declarations

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     The real console.
/// </summary>
/// <remarks>
///     <para>
///         Reading: the async reads of <c>Console.In</c> are synchronous underneath, so a read blocked on the
///         keyboard would ignore Ctrl+C until the user pressed Enter. The read therefore runs on its own
///         thread and the caller waits on it with the token; on cancellation the blocked thread is simply
///         abandoned. It is a background thread and dies with the process, which is what happens next.
///     </para>
///     <para>
///         Writing: a lock-and-unlock notification can arrive while the prompt is on screen and the user is
///         typing. Written naively it would land on the prompt line and leave no prompt after it. So
///         <see cref="Write" /> remembers its text as the pending prompt, and a block written while a prompt
///         is pending first ends the prompt line, then prints, then prints the prompt again. The user still
///         sees "&gt; " and the notification sits on lines of its own.
///     </para>
/// </remarks>
public sealed class SystemConsole : IConsole {

    #region Statics members declarations

    public static SystemConsole Instance { get; } = new();

    #endregion

    #region Fields declarations

    private readonly object  _lock = new();
    private          string? _pendingPrompt;

    #endregion

    /// <inheritdoc />
    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken) {
        string? line;
        try {
            line = await Task.Run(Console.ReadLine, CancellationToken.None).WaitAsync(cancellationToken).ConfigureAwait(false);
        } catch (OperationCanceledException) {
            // The user interrupted while the prompt was waiting, so nobody will ever finish that line:
            // end it here, otherwise "Stopping SignalMe..." would be written right after the "> ".
            lock (_lock) {
                if (_pendingPrompt is not null) { Console.Out.WriteLine(); }
                _pendingPrompt = null;
            }

            throw;
        }

        // On a line the user pressed Enter, which ended the prompt line. At the end of the input nothing
        // did (a redirected stdin echoes nothing), so the line is ended here, as on the cancelled path.
        lock (_lock) {
            if (line is null && _pendingPrompt is not null) { Console.Out.WriteLine(); }
            _pendingPrompt = null;
        }

        return line;
    }

    /// <inheritdoc />
    public void Write(string text) {
        ArgumentNullException.ThrowIfNull(text);

        lock (_lock) {
            Console.Out.Write(text);
            _pendingPrompt = text;
        }
    }

    /// <inheritdoc />
    public void WriteLine(string text) {
        ArgumentNullException.ThrowIfNull(text);

        lock (_lock) { WriteBlock(Console.Out, text); }
    }

    /// <inheritdoc />
    public void WriteError(string line) {
        ArgumentNullException.ThrowIfNull(line);

        lock (_lock) { WriteBlock(Console.Error, line); }
    }

    private void WriteBlock(TextWriter writer, string text) {
        if (_pendingPrompt is null) {
            writer.WriteLine(text);

            return;
        }

        // The prompt lives on the standard output whatever stream the block goes to, so that is where
        // the line is ended and the prompt repeated; the two streams usually share the same terminal.
        Console.Out.WriteLine();
        writer.WriteLine(text);
        Console.Out.Write(_pendingPrompt);
    }

}
