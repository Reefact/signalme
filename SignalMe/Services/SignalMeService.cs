#region Usings declarations

using System;
using System.Threading;
using System.Threading.Tasks;

using Reefact.LuxaforLightingDeviceController;

using SignalMe.Converters;
using SignalMe.Infrastructure;

#endregion

namespace SignalMe.Services;

public sealed class SignalMeService {

    #region Statics members declarations

    /// <summary>
    ///     Indicates whether <paramref name="statusOrMood" /> is a value <see cref="SetAsAsync" /> knows how
    ///     to display.
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

    private readonly ILuxaforDevice     _luxaforDevice;
    private readonly UserCurrentStatus  _userCurrentStatus;
    private readonly IDelay?            _delay;

    #endregion

    #region Constructors declarations

    public SignalMeService(ILuxaforDevice luxaforDevice, UserCurrentStatus? userCurrentStatus = null, IDelay? delay = null) {
        ArgumentNullException.ThrowIfNull(luxaforDevice);

        _luxaforDevice     = luxaforDevice;
        _userCurrentStatus = userCurrentStatus ?? UserCurrentStatus.Default;
        _delay             = delay;
    }

    #endregion

    /// <exception cref="DeviceCommandFailedException">The device refused a command.</exception>
    /// <exception cref="OperationCanceledException">The user interrupted an animation.</exception>
    public Task SetAsAsync(string statusOrMood, CancellationToken cancellationToken) {
        if (UserStatusConverter.TryConvert(statusOrMood, out UserStatus? userStatus)) {
            new UserStatusController(_luxaforDevice, _userCurrentStatus).Display(userStatus.Value);

            return Task.CompletedTask;
        }

        if (UserMoodConverter.TryConvert(statusOrMood, out UserMood? userMood)) {
            return new UserMoodLedController(_luxaforDevice, _userCurrentStatus, _delay).DisplayAsync(userMood.Value, cancellationToken);
        }

        throw new ArgumentException($"Unknown status or mood: '{statusOrMood}'.", nameof(statusOrMood));
    }

    /// <exception cref="DeviceCommandFailedException">The device refused the command.</exception>
    public void TurnOff() {
        // Same rule as Display: forget the remembered status only once the LEDs are actually off.
        _luxaforDevice.TurnOffOrThrow();

        _userCurrentStatus.Set(null);
    }

}
