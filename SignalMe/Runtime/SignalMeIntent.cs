#region Usings declarations

using SignalMe.Services;

#endregion

namespace SignalMe.Runtime;

/// <summary>
///     What a mode asks SignalMe to do. A mode only produces intents: the coordinator decides what the device
///     shows, given the session state and whatever is already running.
/// </summary>
public abstract record SignalMeIntent {

    #region Nested types declarations

    /// <summary>
    ///     Sets the durable status. <see cref="UserStatus.Away" /> is not a desired status, it is what the
    ///     device shows while the session is locked: the coordinator rejects it.
    /// </summary>
    public sealed record SetDesiredStatus(UserStatus Status) : SignalMeIntent;

    /// <summary>Plays a temporary signal over the durable status.</summary>
    public sealed record PlaySignal(UserMood Mood) : SignalMeIntent;

    /// <summary>Turns the device off and forgets the durable status.</summary>
    public sealed record TurnOff : SignalMeIntent;

    #endregion

}
