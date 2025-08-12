#region Usings declarations

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using SignalMe.Services;

#endregion

namespace SignalMe.Infrastructure;

public sealed class UserCurrentStatus {

    private const string FileName = "signalme.ini";

    #region Statics members declarations

    public static UserStatus? Get() {
        string filePath = GetFilePath();

        string? serializedStatus;
        if (!File.Exists(filePath)) {
            serializedStatus = null;
        } else {
            string rawStatus = File.ReadAllText(filePath).Trim();
            serializedStatus = string.IsNullOrWhiteSpace(rawStatus) ? null : rawStatus;
        }
        UserStatus? userStatus = DeSerialize(serializedStatus);

        return userStatus;
    }

    public static void Set(UserStatus? status) {
        string filePath = GetFilePath();
        if (status == null) {
            if (File.Exists(filePath)) {
                File.Delete(filePath);
            }
        } else {
            string serializedStatus = Serialize(status.Value);
            File.WriteAllText(filePath, serializedStatus);
        }
    }

    [return: NotNullIfNotNull(nameof(serializedStatus))]
    private static UserStatus? DeSerialize(string? serializedStatus) {
        if (serializedStatus == null) { return null; }

        return serializedStatus switch {
            UserStatusSerializedValue.Away         => UserStatus.Away,
            UserStatusSerializedValue.Available    => UserStatus.Available,
            UserStatusSerializedValue.Busy         => UserStatus.Busy,
            UserStatusSerializedValue.DoNotDisturb => UserStatus.DoNotDisturb,
            _                                      => throw new ArgumentOutOfRangeException(nameof(serializedStatus), serializedStatus, null)
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

    private static string GetFilePath() {
        string exeDir = AppContext.BaseDirectory;

        return Path.Combine(exeDir, FileName);
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