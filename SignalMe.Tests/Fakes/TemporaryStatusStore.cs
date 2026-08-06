using SignalMe.Infrastructure;

namespace SignalMe.Tests.Fakes;

/// <summary>
///     A status store backed by a throwaway directory, so that tests never touch the real user profile.
/// </summary>
public sealed class TemporaryStatusStore : IDisposable {

    public TemporaryStatusStore() {
        Directory = Path.Combine(Path.GetTempPath(), "signalme-tests", Guid.NewGuid().ToString("N"));
        Store     = new UserCurrentStatus(Directory);
    }

    public string            Directory { get; }
    public UserCurrentStatus Store     { get; }

    public string FilePath => Path.Combine(Directory, "signalme.ini");

    public void Dispose() {
        if (System.IO.Directory.Exists(Directory)) { System.IO.Directory.Delete(Directory, recursive: true); }
    }

}
