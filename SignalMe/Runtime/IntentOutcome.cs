namespace SignalMe.Runtime;

/// <summary>
///     How the coordinator dealt with an intent, reported once it has fully processed it.
/// </summary>
public enum IntentOutcome {

    /// <summary>A status change or a turn-off is done: the device acknowledged it and the status is persisted.</summary>
    Applied,
    /// <summary>The animation ran to its end and the follow-up status is displayed.</summary>
    SignalCompleted,
    /// <summary>The animation was stopped by a lock, another intent, or the shutdown.</summary>
    SignalInterrupted,
    /// <summary>The signal was not played: the session is locked, so the device stays away.</summary>
    SignalRefusedSessionLocked,
    /// <summary>The signal was not played: SignalMe is off, and off has priority over signals.</summary>
    SignalRefusedOff

}
