#region Usings declarations

using SignalMe.Services;

#endregion

namespace SignalMe.Modes.Manual;

/// <summary>
///     What one line typed at the manual prompt means, once parsed. The parser is a pure function over the
///     text and this is all the mode acts on: it never looks at the text again, so what is accepted and
///     what is done with it cannot drift apart.
/// </summary>
public abstract record ManualCommand {

    #region Nested types declarations

    /// <summary>A blank line: nothing to do, the prompt simply comes back.</summary>
    public sealed record Empty : ManualCommand;

    /// <summary>A durable status, typed by its name or one of its aliases.</summary>
    public sealed record SetStatus(UserStatus Status) : ManualCommand;

    /// <summary>A temporary signal to play over the durable status.</summary>
    public sealed record PlaySignal(UserMood Mood) : ManualCommand;

    /// <summary>Asks what SignalMe is showing, and why.</summary>
    public sealed record ShowStatus : ManualCommand;

    /// <summary>Turns the device off and forgets the durable status.</summary>
    public sealed record TurnOff : ManualCommand;

    /// <summary>Lists the commands of the mode.</summary>
    public sealed record Help : ManualCommand;

    /// <summary>
    ///     Anything else, carrying what was typed without its surrounding spaces but in its own case, so
    ///     that the message quotes what the user actually wrote.
    /// </summary>
    public sealed record Unknown(string Input) : ManualCommand;

    #endregion

}
