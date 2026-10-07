#region Usings declarations

using System;
using System.IO;

using SignalMe.Services;

#endregion

namespace SignalMe.Infrastructure;

/// <summary>
///     Remembers the last durable status signalme displayed successfully, so that the next run starts from
///     it.
/// </summary>
/// <remarks>
///     The file holds the desired status, never the effective one: a session lock shows "away" on the
///     device but leaves the file untouched. "away" is still understood on read, because a V1 install may
///     have written it, and it then means "nothing to restore": V2 never asks for away, it applies it by
///     itself while the session is locked. Writing it is a programming error and throws.
/// </remarks>
public sealed class UserCurrentStatus {

    private const string FileName      = "signalme.ini";
    private const string DirectoryName = "SignalMe";

    #region Statics members declarations

    /// <summary>
    ///     The store signalme uses, under the current user's local application data.
    /// </summary>
    public static UserCurrentStatus Default { get; } = new(GetDefaultDirectory());

    /// <summary>
    ///     Reads back a serialized status, treating anything unrecognized as "no status".
    /// </summary>
    /// <remarks>
    ///     An empty or corrupted file must not be fatal: it used to throw, which took down every mood
    ///     command until the file was deleted. Forgetting the remembered status is the recoverable outcome.
    /// </remarks>
    private static UserStatus? DeSerialize(string serializedStatus) {
        return serializedStatus switch {
            UserStatusSerializedValue.Available    => UserStatus.Available,
            UserStatusSerializedValue.Busy         => UserStatus.Busy,
            UserStatusSerializedValue.DoNotDisturb => UserStatus.DoNotDisturb,
            _                                      => null
        };
    }

    private static string Serialize(UserStatus status) {
        string serializedStatus = status switch {
            UserStatus.Available    => UserStatusSerializedValue.Available,
            UserStatus.Busy         => UserStatusSerializedValue.Busy,
            UserStatus.DoNotDisturb => UserStatusSerializedValue.DoNotDisturb,
            _                       => throw new ArgumentOutOfRangeException(nameof(status), status, "Only a durable status can be remembered; away is a presence override, never a desired status.")
        };

        return serializedStatus;
    }

    /// <remarks>
    ///     The file used to sit next to the executable, which is not writable once signalme is installed in
    ///     a shared location: the write threw after the LEDs had already changed.
    /// </remarks>
    private static string GetDefaultDirectory() {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);

        return Path.Combine(localApplicationData, DirectoryName);
    }

    #endregion

    #region Fields declarations

    private readonly string _directory;

    #endregion

    #region Constructors declarations

    /// <summary>
    ///     Remembers the status in <paramref name="directory" />.
    /// </summary>
    /// <remarks>
    ///     signalme itself uses <see cref="Default" />; taking the directory as an argument is what lets the
    ///     tests point at a temporary folder instead of the real user profile.
    /// </remarks>
    public UserCurrentStatus(string directory) {
        ArgumentNullException.ThrowIfNull(directory);

        _directory = directory;
    }

    #endregion

    public UserStatus? Get() {
        string filePath = GetFilePath();
        if (!File.Exists(filePath)) { return null; }

        string rawStatus = File.ReadAllText(filePath).Trim();

        return DeSerialize(rawStatus);
    }

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="status" /> is <see cref="UserStatus.Away" />.</exception>
    public void Set(UserStatus? status) {
        string filePath = GetFilePath();
        if (status == null) {
            if (File.Exists(filePath)) { File.Delete(filePath); }

            return;
        }

        // Serialized before the directory is touched, so a rejected status leaves no trace behind.
        string serializedStatus = Serialize(status.Value);

        Directory.CreateDirectory(_directory);

        // Written aside and moved into place, so an interruption mid-write leaves the previous status
        // intact rather than a half-written file. The move is atomic within a volume, and both paths sit
        // in the same directory.
        string temporaryPath = filePath + ".tmp";
        File.WriteAllText(temporaryPath, serializedStatus);
        File.Move(temporaryPath, filePath, overwrite: true);
    }

    private string GetFilePath() {
        return Path.Combine(_directory, FileName);
    }

    #region Nested types declarations

    private static class UserStatusSerializedValue {

        public const string Available    = "available";
        public const string Busy         = "busy";
        public const string DoNotDisturb = "do-not-disturb";

    }

    #endregion

}
