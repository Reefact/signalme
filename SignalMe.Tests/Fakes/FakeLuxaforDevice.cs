using Reefact.LuxaforLightingDeviceController;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     A Luxafor device that records what it is asked to do, and that can be told to refuse commands or to
///     break outright.
/// </summary>
/// <remarks>
///     <see cref="ILuxaforDevice" /> is already an interface, so no extra abstraction is needed to drive
///     signalme without hardware.
/// </remarks>
public sealed class FakeLuxaforDevice : ILuxaforDevice {

    private int _calls;

    /// <summary>Every command the device accepted, in order.</summary>
    public List<string> Commands { get; } = new();

    /// <summary>Refuse every command from this call number on (1-based). Null never refuses.</summary>
    public int? RefuseFromCall { get; set; }

    /// <summary>Throw <see cref="Failure" /> on this call number (1-based). Null never throws.</summary>
    public int? ThrowOnCall { get; set; }

    /// <summary>The exception <see cref="ThrowOnCall" /> raises. Deliberately not a device failure, so that
    ///     tests can tell an animation's own error apart from a refused command.</summary>
    public Exception Failure { get; set; } = new InvalidOperationException("USB write failed");

    public bool IsDisposed { get; private set; }

    public string? LastCommand => Commands.Count == 0 ? null : Commands[^1];

    public string Description => "fake Luxafor device";

    /// <summary>Settable, so that a selection test can tell several devices apart by their id.</summary>
    public string Path { get; set; } = @"\\?\fake";

    public bool SetColor(BrightColor color)                              => Accept($"SetColor({color})");
    public bool SetColor(TargetedLeds targetedLeds, BrightColor color)   => Accept($"SetColor({targetedLeds}, {color})");
    public bool TurnOff()                                                => Accept("TurnOff");
    public bool TurnOff(TargetedLeds targetedLeds)                       => Accept($"TurnOff({targetedLeds})");
    public bool Send(LightingCommand command)                            => Accept($"Send({command})");
    public bool FadeColor(BrightColor color, FadeDuration duration)                             => Accept("FadeColor");
    public bool FadeColor(TargetedLeds targetedLeds, BrightColor color, FadeDuration duration)   => Accept("FadeColor");
    public bool Strobe(BrightColor color, Speed speed, Repeat repeat)                            => Accept("Strobe");
    public bool Strobe(TargetedLeds targetedLeds, BrightColor color, Speed speed, Repeat repeat) => Accept("Strobe");

    public bool PlayPattern(WavePattern wavePattern, BrightColor color, Speed speed, Repeat repeat) => Accept("PlayPattern");
    public bool PlayPattern(BuiltInPattern pattern, Repeat repeat)                                  => Accept("PlayPattern");

    public void Dispose() {
        IsDisposed = true;
    }

    private bool Accept(string command) {
        _calls++;
        if (ThrowOnCall == _calls) { throw Failure; }
        if (RefuseFromCall is not null && _calls >= RefuseFromCall) { return false; }

        Commands.Add(command);

        return true;
    }

}
