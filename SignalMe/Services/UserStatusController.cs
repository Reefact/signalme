#region Usings declarations

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Infrastructure;

#endregion

namespace SignalMe.Services;

public sealed class UserStatusController {

    #region Statics members declarations

    private static readonly Dictionary<UserStatus, BrightColor> _colorByUserStatus;

    static UserStatusController() {
        _colorByUserStatus = new Dictionary<UserStatus, BrightColor> {
            { UserStatus.Available, PredefinedColor.Available },
            { UserStatus.Away, PredefinedColor.Away },
            { UserStatus.Busy, PredefinedColor.Busy },
            { UserStatus.DoNotDisturb, PredefinedColor.DoNotDisturb }
        };
    }

    #endregion

    #region Fields declarations

    private readonly UserCurrentStatus _userCurrentStatus;

    #endregion

    #region Constructors declarations

    /// <param name="luxaforDevice">The device to drive.</param>
    /// <param name="userCurrentStatus">
    ///     Where the durable status is remembered. Defaults to <see cref="UserCurrentStatus.Default" />;
    ///     the tests pass a store pointing at a temporary directory.
    /// </param>
    public UserStatusController(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        Device             = luxaforDevice;
        _userCurrentStatus = userCurrentStatus ?? UserCurrentStatus.Default;
    }

    #endregion

    public ILuxaforDevice Device { get; }

    public BrightColor GetUserStatusColor(UserStatus? userStatus) {
        if (userStatus == null) { return BrightColor.Black; }

        if (!Enum.IsDefined(typeof(UserStatus), userStatus)) { throw new InvalidEnumArgumentException(nameof(userStatus), (int)userStatus, typeof(UserStatus)); }

        return _colorByUserStatus[userStatus.Value];
    }

    /// <summary>
    ///     Displays <paramref name="status" /> on the device and remembers it, or turns the device off when
    ///     no status is given.
    /// </summary>
    /// <exception cref="DeviceCommandFailedException">The device refused the command.</exception>
    public void Display(UserStatus? status) {
        if (status == null) {
            Device.TurnOffOrThrow();

            return;
        }

        if (!Enum.IsDefined(typeof(UserStatus), status)) { throw new InvalidEnumArgumentException(nameof(status), (int)status, typeof(UserStatus)); }

        if (!_colorByUserStatus.TryGetValue(status.Value, out BrightColor? statusColor)) { throw new InvalidOperationException($"No color is configured for the '{status.Value}' status."); }

        // The status is remembered only once the device has acknowledged the write, otherwise the
        // remembered status would describe a color the LEDs never displayed.
        Device.SetColorOrThrow(statusColor, $"display the '{status.Value}' status");

        _userCurrentStatus.Set(status);
    }

    /// <summary>
    ///     Runs a temporary animation, then puts the durable status back.
    /// </summary>
    /// <param name="statusOnFailure">The status to fall back on when the animation does not complete.</param>
    /// <param name="animation">
    ///     The animation to play. It returns the status to leave behind once it has completed, which is
    ///     <paramref name="statusOnFailure" /> for every mood except "ready", the one that deliberately
    ///     ends on another status.
    /// </param>
    /// <remarks>
    ///     <para>The four outcomes this method exists to get right:</para>
    ///     <list type="bullet">
    ///         <item>animation succeeds, restore succeeds: the call returns normally.</item>
    ///         <item>
    ///             animation succeeds, restore fails: the restore failure IS the command's failure and is
    ///             thrown, because a command that leaves the LEDs on an animation color has not done its job.
    ///         </item>
    ///         <item>animation fails, restore succeeds: the animation's error is rethrown untouched.</item>
    ///         <item>
    ///             animation fails, restore fails too: the animation's error still wins. The restore failure
    ///             is reported on the error output rather than thrown, so it cannot bury the original cause.
    ///         </item>
    ///     </list>
    /// </remarks>
    /// <exception cref="DeviceCommandFailedException">
    ///     The animation completed but the durable status could not be restored.
    /// </exception>
    public void PlayAndRestore(UserStatus? statusOnFailure, Func<UserStatus?> animation) {
        ArgumentNullException.ThrowIfNull(animation);

        UserStatus? statusToRestore;
        try {
            statusToRestore = animation();
        } catch {
            RestoreQuietly(statusOnFailure);

            throw;
        }

        Display(statusToRestore);
    }

    /// <inheritdoc cref="PlayAndRestore(UserStatus?, Func{UserStatus?})" />
    public void PlayAndRestore(UserStatus? statusToRestore, Action animation) {
        ArgumentNullException.ThrowIfNull(animation);

        PlayAndRestore(statusToRestore, () => {
            animation();

            return statusToRestore;
        });
    }

    public UserStatus? GetUserCurrentStatus() {
        return _userCurrentStatus.Get();
    }

    /// <summary>
    ///     Restores the status while an error is already on its way up, reporting a second failure instead of
    ///     throwing it.
    /// </summary>
    private void RestoreQuietly(UserStatus? status) {
        try {
            Display(status);
        } catch (Exception exception) {
            Console.Error.WriteLine($"The previous status could not be restored either: {exception.Message}");
        }
    }

}
