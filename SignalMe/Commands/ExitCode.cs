namespace SignalMe.Commands;

/// <summary>
///     The process exit codes returned by signalme. They are part of its contract: scripts can rely on them.
/// </summary>
public static class ExitCode {

    #region Statics members declarations

    /// <summary>The command completed and the device acknowledged it.</summary>
    public const int Success = 0;
    /// <summary>The command line was rejected: unknown status or mood.</summary>
    public const int UsageError = 1;
    /// <summary>No Luxafor device could be reached, or the device refused a command.</summary>
    public const int DeviceError = 2;
    /// <summary>An unexpected error occurred.</summary>
    public const int UnexpectedError = 3;
    /// <summary>The user interrupted an animation with Ctrl+C. The previous status was restored.</summary>
    public const int Cancelled = 4;

    #endregion

}
