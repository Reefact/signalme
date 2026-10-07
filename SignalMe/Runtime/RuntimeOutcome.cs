namespace SignalMe.Runtime;

/// <summary>
///     How the runtime ended, which the command line turns into an exit code.
/// </summary>
public enum RuntimeOutcome {

    /// <summary>Stopped cleanly: the token was cancelled or the mode returned.</summary>
    Stopped,
    /// <summary>The device refused a command or could not be reached.</summary>
    DeviceFailed,
    /// <summary>The coordinator or the mode failed for any other reason.</summary>
    Faulted

}
