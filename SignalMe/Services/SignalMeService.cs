#region Usings declarations

using System;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Converters;
using SignalMe.Infrastructure;

#endregion

namespace SignalMe.Services;

public sealed class SignalMeService {

    #region Statics members declarations

    /// <summary>
    ///     Indicates whether <paramref name="statusOrMood" /> is a value <see cref="SetAs" /> knows how to
    ///     display.
    /// </summary>
    /// <remarks>
    ///     Lets the CLI reject a typo before a device is acquired, so the user is told about their typo
    ///     rather than about the hardware.
    /// </remarks>
    public static bool IsKnown(string statusOrMood) {
        ArgumentNullException.ThrowIfNull(statusOrMood);

        return UserStatusConverter.TryConvert(statusOrMood, out _) || UserMoodConverter.TryConvert(statusOrMood, out _);
    }

    #endregion

    #region Fields declarations

    private readonly ILuxaforDevice    _luxaforDevice;
    private readonly UserCurrentStatus _userCurrentStatus;

    #endregion

    #region Constructors declarations

    public SignalMeService(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice     = luxaforDevice;
        _userCurrentStatus = userCurrentStatus ?? UserCurrentStatus.Default;
    }

    #endregion

    public void SetAs(string statusOrMood) {
        if (UserStatusConverter.TryConvert(statusOrMood, out UserStatus? userStatus)) {
            new UserStatusController(_luxaforDevice, _userCurrentStatus).Display(userStatus.Value);

            return;
        }

        if (UserMoodConverter.TryConvert(statusOrMood, out UserMood? userMood)) {
            new UserMoodLedController(_luxaforDevice, _userCurrentStatus).Display(userMood.Value);

            return;
        }

        throw new ArgumentException($"Unknown user status or mood : {statusOrMood}");
    }

    /// <exception cref="DeviceCommandFailedException">The device refused the command.</exception>
    public void TurnOff() {
        // Same rule as Display: forget the remembered status only once the LEDs are actually off.
        _luxaforDevice.TurnOffOrThrow();

        _userCurrentStatus.Set(null);
    }

}
