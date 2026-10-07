#region Usings declarations

using SignalMe.Services;
using SignalMe.Sessions;

#endregion

namespace SignalMe.Runtime;

/// <summary>
///     An immutable snapshot of what SignalMe is doing, published by the coordinator after every message it
///     handles so that a mode can read it from any thread without a lock.
/// </summary>
/// <param name="DesiredStatus">The durable status the mode asked for, or null when SignalMe is off.</param>
/// <param name="Session">The state of the Windows session as last reported.</param>
/// <param name="Effective">What the device shows for that combination.</param>
/// <param name="PlayingSignal">The signal currently animating the LEDs, if any.</param>
public sealed record SignalMeState(UserStatus? DesiredStatus, SessionState Session, EffectiveStatus Effective, UserMood? PlayingSignal);
