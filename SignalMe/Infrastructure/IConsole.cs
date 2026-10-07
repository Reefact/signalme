#region Usings declarations

using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     Every console interaction goes through this, so that the tests can script the input and read the
///     output. <see cref="Write" />, <see cref="WriteLine" /> and <see cref="WriteError" /> may be called
///     from several threads at once: the coordinator reports a lock while the mode is waiting for input.
/// </summary>
public interface IConsole {

    /// <summary>
    ///     Reads one line; null at end of input. Throws <see cref="System.OperationCanceledException" />
    ///     when the token is cancelled while waiting.
    /// </summary>
    Task<string?> ReadLineAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Writes a prompt, without a newline, and marks it as pending until the next
    ///     <see cref="ReadLineAsync" /> completes.
    /// </summary>
    void Write(string text);

    /// <summary>Writes a block that may span several lines, followed by a newline.</summary>
    void WriteLine(string text);

    /// <summary>Writes a line on the error output.</summary>
    void WriteError(string line);

}
