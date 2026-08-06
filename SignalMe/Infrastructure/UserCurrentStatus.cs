#region Usings declarations

using System;
using System.IO;

using SignalMe.Services;

#endregion

namespace SignalMe.Infrastructure;

public sealed class UserCurrentStatus {

    private const string FileName      = "signalme.ini";
    private const string DirectoryName = "SignalMe";

    #region Statics members declarations

    public static UserStatus? Get() {
        string filePath = GetFilePath();
        if (!File.Exists(filePath)) { return null; }

        string rawStatus = File.ReadAllText(filePath).Trim();

        return DeSerialize(rawStatus);
    }

    public static void Set(UserStatus? status) {
        string filePath = GetFilePath();
        if (status == null) {
            if (File.Exists(filePath)) {
                File.Delete(filePath);
            }
        } else {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            string serializedStatus = Serialize(status.Value);
            File.WriteAllText(filePath, serializedStatus);
        }
    }

    /// <summary>
    ///     Reads back a serialized status, treating anything unrecognized as "no status".
    /// </summary>
    /// <remarks>
    ///     An empty or corrupted file must not be fatal: it used to throw, which took down every mood
    ///     command until the file was deleted. Forgetting the remembered status is the recoverable outcome.
    /// </remarks>
    private static UserStatus? DeSerialize(string serializedStatus) {
        return serializedStatus switch {
            UserStatusSerializedValue.Away         => UserStatus.Away,
            UserStatusSerializedValue.Available    => UserStatus.Available,
            UserStatusSerializedValue.Busy         => UserStatus.Busy,
            UserStatusSerializedValue.DoNotDisturb => UserStatus.DoNotDisturb,
            _                                      => null
        };
    }

    private static string Serialize(UserStatus status) {
        string serializedStatus = status switch {
            UserStatus.Away         => UserStatusSerializedValue.Away,
            UserStatus.Available    => UserStatusSerializedValue.Available,
            UserStatus.Busy         => UserStatusSerializedValue.Busy,
            UserStatus.DoNotDisturb => UserStatusSerializedValue.DoNotDisturb,
            _                       => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        return serializedStatus;
    }

    /// <summary>
    ///     Locates the file remembering the last durable status, under the current user's local
    ///     application data.
    /// </summary>
    /// <remarks>
    ///     It used to sit next to the executable, which is not writable once signalme is installed in a
    ///     shared location: the write threw and the command died after the LEDs had already changed.
    /// </remarks>
    private static string GetFilePath() {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);

        return Path.Combine(localApplicationData, DirectoryName, FileName);
    }

    #endregion

    #region Nested types declarations

    private static class UserStatusSerializedValue {

        public const string Away         = "away";
        public const string Available    = "available";
        public const string Busy         = "busy";
        public const string DoNotDisturb = "do-not-disturb";

    }

    #endregion

}
