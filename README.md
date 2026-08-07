# SignalMe

[![CI](https://github.com/Reefact/signalme/actions/workflows/ci.yml/badge.svg)](https://github.com/Reefact/signalme/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/SignalMe.svg)](https://www.nuget.org/packages/SignalMe)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

SignalMe is a tiny command-line companion for Luxafor devices. Set your availability, trigger expressive
light signals, and automate your workplace status without a GUI.

```shell
signalme as busy      # solid yellow, and it stays there
signalme as happy     # a pastel rainbow, then straight back to busy
signalme status       # busy
signalme off          # lights out
```

## Install

SignalMe is distributed as a .NET tool. It needs the [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

```shell
dotnet tool install --global SignalMe
```

Then use it as `signalme` from any shell. To update or remove it:

```shell
dotnet tool update --global SignalMe
dotnet tool uninstall --global SignalMe
```

## Usage

```shell
signalme as <status-or-mood>   # set a durable status, or play a temporary signal
signalme status                # print the durable status SignalMe last set
signalme off                   # turn every LED off  (alias: switch-off)
signalme --help
```

## Statuses

A status is durable: the LEDs hold it until you change it.

| Value | Alias | Colour |
| --- | --- | --- |
| `available` | `free` | green |
| `busy` | | yellow |
| `away` | | purple |
| `do-not-disturb` | `dnd` | red |

## Temporary signals

A signal plays an animation for a few seconds, then puts your durable status back exactly as it was.

| Value | What it looks like |
| --- | --- |
| `happy` | fades to pastel, runs a rainbow wave, fades back |
| `bored` | drifts through deep purples, LED by LED |
| `desperate` | blinks S-O-S in white |
| `warning` | red and blue alternating front to back, like an emergency light |
| `alerting` | twenty fast red flashes |
| `ready` | **the exception**: announces you are available again, and ends on the `available` status |

`ready` is the only signal that changes your durable status. Everything else restores what was there.

## How it works

- **Restoring.** Before an animation starts, SignalMe reads the durable status it remembers and puts it
  back when the animation ends — including when the animation fails, and when you interrupt it with
  Ctrl+C. If the restore itself fails, that is an error, not a warning: the command exits non-zero rather
  than leaving your LEDs stuck on an animation colour.
- **Local storage.** The durable status is remembered in a single small text file under your local
  application data (`%LOCALAPPDATA%\SignalMe\signalme.ini` on Windows). It is written to a temporary file
  and moved into place, so an interruption mid-write cannot corrupt it. Delete it and SignalMe simply
  forgets; nothing else reads or writes it.
- **`status` does not read the device.** Luxafor devices cannot be queried, so `signalme status` reports
  what SignalMe last asked for, not what the LEDs are physically showing. It works with the device
  unplugged.
- **No device, no silence.** If no device is plugged in, or another application is holding it, SignalMe
  says so on stderr and exits with a device error. It never reports success for a command the device
  refused.

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | success |
| `1` | usage error — unknown status or mood |
| `2` | device error — none found, or the device refused a command |
| `3` | unexpected error |
| `4` | interrupted with Ctrl+C; the previous status was restored |

## Supported hardware and platforms

**Windows only.** SignalMe drives the device through
[Reefact.LuxaforLightingDeviceController](https://github.com/Reefact/luxafor-lighting-device-controller),
which talks to the Windows HID stack. The tool installs anywhere .NET runs, but it can only find and drive
a device on Windows.

| Device | Status |
| --- | --- |
| Luxafor Orb | **Tested** — the device SignalMe is developed against |
| Luxafor Flag | Expected to work; same identifiers and lighting protocol, not tested |
| Luxafor Mute Button, Colorblind Flag | Expected to work; LED layout and colour rendering may differ |
| Bluetooth, Switch, Cube, Pomodoro-Timer, CO2 Monitor | Not supported — they do not use this USB HID protocol |

Feedback on an untested device is welcome: please [open an issue](https://github.com/Reefact/signalme/issues).

## Development

```shell
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release -o artifacts
```

The whole test suite runs in under a second and needs no hardware: it drives SignalMe through a fake
`ILuxaforDevice`, and animations use an injected delay so nothing waits for real.

To try a local build as a tool:

```shell
dotnet pack -c Release -o artifacts
dotnet tool install --global SignalMe --add-source ./artifacts --version 0.1.0
```

## License

[Apache-2.0](LICENSE)
