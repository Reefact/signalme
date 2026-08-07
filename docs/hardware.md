_[Version française](hardware-FR.md)_

# Hardware and platform support

## Platform

**Windows only.**

SignalMe drives the device through
[Reefact.LuxaforLightingDeviceController](https://github.com/Reefact/luxafor-lighting-device-controller),
which talks to the Windows HID stack. The tool installs anywhere .NET runs, and commands that do not touch
the device — `--help`, `status` — work anywhere, but no device can be found or driven outside Windows.

On another platform, `signalme as busy` exits with a device error and says why.

## Devices

| Device | Status |
| --- | --- |
| Luxafor Orb | **Tested.** The device SignalMe is developed against. |
| Luxafor Flag | **Expected to work, not tested.** Same identifiers and lighting protocol, six addressable LEDs. |
| Luxafor Mute Button, Luxafor Colorblind Flag | **Expected to work, not tested.** The lighting commands are the same; the LED layout, the number of LEDs and the colour rendering may differ. |
| Luxafor Bluetooth, Switch, Cube, Pomodoro-Timer, CO2 Monitor | **Not supported.** These are not driven through this USB HID protocol. |

"Expected to work" means the protocol matches and nothing is known to be wrong — not that anyone has run
SignalMe against one. Feedback on an untested device is welcome: please
[open an issue](https://github.com/Reefact/signalme/issues).

## Multiple devices

SignalMe uses the first Luxafor device the controller library discovers.

Explicit device selection is not supported in SignalMe 1.0. With several devices plugged in, which one
responds is whichever the library enumerates first, and that order is not something SignalMe controls or
guarantees to be stable.

## Limitations

- **The device cannot be read back.** Luxafor devices accept commands but do not report their LED state,
  which is why `signalme status` reports what SignalMe last requested rather than what is lit. See
  [Command reference](commands.md#signalme-status).
- **One application at a time.** The device is held while a command runs. If another application owns it,
  SignalMe reports a device error rather than waiting.
- **No brightness control.** The protocol exposes colours, not brightness levels.
