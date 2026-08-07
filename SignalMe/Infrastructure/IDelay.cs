#region Usings declarations

using System;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     Waits between the frames of an animation.
/// </summary>
/// <remarks>
///     The only reason this exists: animations are mostly waiting, so the tests would spend fifteen seconds
///     sitting through delays that prove nothing. Production waits for real, tests return immediately, and
///     both honour cancellation.
/// </remarks>
public interface IDelay {

    Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken);

}

public static class DelayExtensions {

    #region Statics members declarations

    /// <summary>Waits, in milliseconds, which is how the animations express their timings.</summary>
    public static Task WaitAsync(this IDelay delay, int milliseconds, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(delay);

        return delay.WaitAsync(TimeSpan.FromMilliseconds(milliseconds), cancellationToken);
    }

    #endregion

}

/// <summary>
///     Waits for real.
/// </summary>
public sealed class RealDelay : IDelay {

    #region Statics members declarations

    public static RealDelay Instance { get; } = new();

    #endregion

    /// <inheritdoc />
    public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken) {
        return Task.Delay(duration, cancellationToken);
    }

}
