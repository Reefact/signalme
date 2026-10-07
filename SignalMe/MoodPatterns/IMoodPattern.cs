#region Usings declarations

using System.Threading;
using System.Threading.Tasks;

using SignalMe.Services;

#endregion

namespace SignalMe.MoodPatterns;

/// <summary>
///     A pure animation: it drives the LEDs, never persists anything, never restores anything. What to show
///     once it is over is the coordinator's decision, which is why the pattern only says which status to
///     leave behind.
/// </summary>
public interface IMoodPattern {

    /// <summary>
    ///     Plays the animation over <paramref name="baseStatus" />, the durable status it starts from, and
    ///     returns the status to leave behind: <paramref name="baseStatus" /> for every mood except "ready",
    ///     which announces availability and returns <see cref="UserStatus.Available" />.
    /// </summary>
    Task<UserStatus?> PlayAsync(UserStatus? baseStatus, CancellationToken cancellationToken);

}
