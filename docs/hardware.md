_[Version française](hardware-FR.md)_

# Hardware and platform support

## Platform

**Windows only.**

SignalMe drives the device through
[Reefact.LuxaforLightingDeviceController](https://github.com/Reefact/luxafor-lighting-device-controller),
which talks to the Windows HID stack, and watches the lock state of the Windows session it runs in. The
tool installs anywhere .NET runs, and `--help` and `--version` work anywhere, but no device can be found
or driven outside Windows.

On another platform, `signalme` exits with a device error and says why.

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

SignalMe drives one device per run, and looks for devices once, at startup. With one device plugged in
it is used without a question. With several, SignalMe asks which one:

```text
$ signalme
SignalMe 2.0.0-preview.1

2 Luxafor devices detected.

┌───┬──────────────────────────────┐
│ # │ Id                           │
├───┼──────────────────────────────┤
│ 1 │ <device-id-1>                │
│ 2 │ <device-id-2>                │
└───┴──────────────────────────────┘

Select device: 1

Identifying device #1...

Use this device? [Y/N]: n

Select device: 2

Identifying device #2...

Use this device? [Y/N]: y

Device selected: <device-id-2>
```

- `#` is a number SignalMe assigns for this selection only. `Id` is the identifier the HID layer reports
  for the device: it tells two devices apart but is not meant to be read, which is why SignalMe shows
  you the device instead.
- Answering `Select device:` with a number plays a short **identification wave** on that device — a white
  chase across its LEDs, twice — and turns it off again. It is not a status: nothing is remembered.
- `Use this device? [Y/N]:` takes `y` or `n`, in any case. `n` goes back to the selection, and picking a
  device again replays the wave. `y` keeps it for the whole run; every other device is released at once.
- If the device fails during the wave, the error is printed and the selection is asked again — pick
  another device or retry.
- An empty, non-numeric or out-of-range answer gets `Invalid device number.` and the question again; any
  other answer to the confirmation is asked again too. Nothing you type here can stop SignalMe except
  Ctrl+C or closing the input, both of which exit cleanly.

The order of the table is the order the library enumerates the devices in, which SignalMe neither
controls nor guarantees to be stable — hence the wave.

## Session monitoring

SignalMe follows the lock state of the Windows session it was launched in, through the session switch
notifications Windows delivers to that session. Only that session counts: another user's lock on the same
machine is not seen, and a lock is not seen either when SignalMe runs somewhere else than in your
interactive session — started as a service, or by a scheduled task running in the background (session 0),
it never learns that you locked your screen. Run it from a terminal in your own session.

On a platform other than Windows the session is assumed active and never changes; the device discovery
is what stops SignalMe there anyway.

## Limitations

- **The device cannot be read back.** Luxafor devices accept commands but do not report their LED state,
  which is why the `status` command reports what SignalMe asked for rather than what is lit. See
  [Command reference](commands.md#status).
- **One application at a time.** The device is held for the whole run. If another application owns it
  when SignalMe starts, SignalMe reports a device error rather than waiting; while SignalMe runs, that
  other application cannot drive it.
- **No reconnection.** A device unplugged during the run ends it: SignalMe checks every two seconds that
  the device is still plugged in, prints `Luxafor device disconnected.` and exits with a device error,
  without waiting for the next command. A device taken over by another application, or unplugged and
  plugged back between two checks, ends the run at the next write instead: the handle SignalMe holds
  does not survive even a short absence. Plug the device back and start SignalMe again.
- **No brightness control.** The protocol exposes colours, not brightness levels.
