namespace SignalMe.Commands;

/// <summary>
///     What the command line asks for, once read. The parser is a pure function over the arguments and
///     this is all the command app acts on, so what is accepted and what is done with it cannot drift apart.
/// </summary>
public abstract record CommandLineRequest {

    #region Nested types declarations

    /// <summary>Prints the usage and exits, without looking for a device.</summary>
    public sealed record ShowHelp : CommandLineRequest;

    /// <summary>Prints the version and exits, without looking for a device.</summary>
    public sealed record ShowVersion : CommandLineRequest;

    /// <summary>Starts the resident runtime in the named mode, as typed: the mode factory resolves it.</summary>
    public sealed record Run(string Mode) : CommandLineRequest;

    /// <summary>A usage error, carrying the sentence that names what was wrong.</summary>
    public sealed record Invalid(string Reason) : CommandLineRequest;

    #endregion

}
