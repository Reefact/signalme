namespace SignalMe.Runtime;

/// <summary>
///     What the device shows once the session state has been applied to the desired status. A running
///     signal is reported separately, through <see cref="SignalMeState.PlayingSignal" />.
/// </summary>
public enum EffectiveStatus {

    Off,
    Away,
    Available,
    Busy,
    DoNotDisturb

}
