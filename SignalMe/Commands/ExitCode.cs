namespace SignalMe.Commands;

/// <summary>
///     The process exit codes returned by signalme. They are part of its contract: scripts can rely on them.
/// </summary>
public static class ExitCode {

    #region Statics members declarations

    /// <summary>SignalMe ran and stopped cleanly: Ctrl+C, end of input, or the mode returned.</summary>
    public const int Success = 0;
    /// <summary>The command line was rejected: unknown mode, unknown option, missing value or stray argument.</summary>
    public const int UsageError = 1;
    /// <summary>No Luxafor device could be reached, or the device refused a command during the run.</summary>
    public const int DeviceError = 2;
    /// <summary>An unexpected error occurred.</summary>
    public const int UnexpectedError = 3;

    #endregion

}
