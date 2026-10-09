_[Version française](CHANGELOG-FR.md)_

# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0-preview.2] - 2026-10-09

The second preview of 2.0. SignalMe now notices an unplugged device on its own, within two seconds,
instead of finding out at the next write. Like the first preview, it is published as a pre-release:
`dotnet tool install --global SignalMe --prerelease` installs it, and
`dotnet tool update --global SignalMe --prerelease` updates a preview already installed.

### Added

- **An unplugged device is noticed at once.** SignalMe checks every two seconds that the device it drives
  is still plugged in. When it is gone, SignalMe prints `Luxafor device disconnected.` and exits with the
  device error code `2`, instead of carrying on until the next command, lock or signal failed with a
  `refused` error that did not say why. Nothing is written to the device on the way out, so no turn-off
  error follows. There is still no reconnection: plug the device back and start SignalMe again.

### Changed

- **Built on Reefact.LuxaforLightingDeviceController 2.1.0**, whose `ILuxaforDevice.IsConnected` is what
  the check above asks: whether the device path is still among the HID devices present, without opening
  or writing anything.
- The test suite asserts with NFluent, like the Luxafor library's, instead of xUnit's `Assert`.

## [2.0.0-preview.1] - 2026-10-08

The first preview of a new major version. SignalMe stops being a one-shot command and becomes a resident
process: it runs in your terminal, you type statuses and signals at its prompt, and it switches the device
to `away` by itself while your Windows session is locked. The command line of 1.x no longer exists, which
is what the major version is for.

A preview rather than 2.0.0 because the behaviour is complete and tested, but without hardware: the suite
drives a fake device and a fake session. This release exists to be run against real Luxafor devices and
real lock screens before the version is made final. It is published as a pre-release, so
`dotnet tool install --global SignalMe` keeps installing 1.x unless `--prerelease` is added.

### Added

- **Automatic `away` on session lock.** SignalMe watches the Windows session it runs in: a lock shows
  `away` on the device, an unlock brings your status back, and the status you asked for is never changed
  by either. A lock interrupts a signal that is playing, which does not resume. Only the session SignalMe
  runs in is watched: started as a service or in a background session, it sees no lock.
- **Device selection.** With several Luxafor devices plugged in, SignalMe lists them, plays a short white
  wave on the one you pick so you can see which it is, and asks for a confirmation before using it. The
  devices not retained are released. With one device nothing is asked, as before.
- **The `manual` mode**, the default and the only one for now, in which the status is typed at SignalMe's
  prompt: the statuses and signals of 1.x, plus `status` (mode, desired status, effective status, session
  state), `off` and `help`. Unknown input is reported and SignalMe keeps running. `--mode <MODE>` names
  the mode, so that other sources of status can be added later without changing the core.
- **`--version`**, which prints the version and exits, like `--help` without touching a device.
- **A clean stop on end of input.** Closing the console input stops SignalMe the same way Ctrl+C does:
  device off, exit code `0`.
- **Package validation now checks the icon against nuget.org's 1 MB limit.** The constraint was written
  down in the development guide but nothing enforced it, and nuget.org only applies it when the package is
  pushed — after the tag is cut and the release job is already running. It is now a build failure, where a
  too-heavy icon costs nothing but a smaller file.
- **Tests for the build scripts**, in Pester, under `build/tests`, run by `build/Test-BuildScripts.ps1` in
  both workflows. `Validate-Package.ps1` is what stands between a broken package and an immutable
  published version, and it only earns that place if it actually fails: the suite hands it synthetic
  packages that are wrong in exactly one way — an icon of exactly 1 MB, a missing assembly, a renamed
  command, a leaked source file — and checks it rejects each of them.

### Changed

- **SignalMe is resident.** `signalme` starts it and it stays until Ctrl+C: it finds the device once,
  shows the status remembered from the last run, then applies what you type and what the session does.
  It turns the device off when it stops, so that a lit device never suggests it is still watching your
  presence. One component writes to the device, ever; a signal, a status change and a lock can no longer
  race each other.
- **The command line is incompatible with 1.x.** `signalme [--mode <MODE>]`, `--help` and `--version` are
  all it accepts; an unknown option, an unexpected argument or a missing option value is a usage error.
  Statuses and signals are now interactive commands.
- **A signal needs a durable status.** With SignalMe off, a signal is refused with
  `SignalMe is off: '<signal>' is not played.` rather than played over a dark device, and `ready` can no
  longer turn "no status" into `available`. Off has priority over everything, the lock comes next: a
  signal is refused while the session is locked too.
- **A signal interrupted is not restored by the signal.** Restoration belongs to the runtime: whatever
  interrupts a signal — a lock, Ctrl+C, a new request from the mode — decides what the device shows next.
  `ready` still ends on `available`, and still only when it completes.
- **Exit codes.** Ctrl+C now exits with `0`: stopping SignalMe is the normal way to use it, not an
  interruption. `1` covers every command-line error (unknown mode included), `2` every device failure,
  before or during the run, and `3` the unexpected.
- **A device failure during the run stops SignalMe**, with exit code `2`, after turning the device off
  as far as it still can. A status the device refused is not remembered.
- **The status file is read more carefully.** It still holds the last durable status the device showed,
  never the `away` a lock produces. A file left by 1.x that says `away` is read as "no status".
- **The package description** now describes a resident presence runtime.

### Removed

- **The `as`, `status` and `off` commands** of the command line, and the `switch-off` alias. Their job
  is done at SignalMe's prompt: a status or a signal by its name, `status` and `off`.
- **Exit code `4`** (interrupted by the user). Ctrl+C exits with `0`.
- **`away` as a status you set.** It is the presence override SignalMe applies while the session is
  locked; typed at the prompt it is an unknown command.

## [1.0.2] - 2026-08-07

Another icon-only release, for the same reason as the last one: a published NuGet package is immutable, so
a new icon needs a version of its own. Same tool, same commands, same behaviour.

### Changed

- **A new package icon** — a glowing orb, the thing SignalMe actually lights up. It replaces the 1.0.1
  lighthouse, which came from Flaticon under a licence requiring the design to be credited wherever it
  appears. A package icon carries no credit line of its own, so the README had to carry it on the
  package's behalf. The icon is now the project's own and depends on nobody else's terms, which is why the
  attribution section is gone from both READMEs — it existed only to carry that credit.

### Removed

- **`build/make-icon.py`**, which drew the lighthouse from vector primitives. The icon is a committed
  image now, not a generated one, so a generator that produces the old design would only be misleading.
  The constraints a replacement icon has to meet are written down in the development guide instead.

## [1.0.1] - 2026-08-07

An icon-only release. NuGet packages are immutable, so the icon could not reach the published 1.0.0 and
needed a version of its own. Same tool, same commands, same behaviour.

### Added

- **A package icon** — a lighthouse — so SignalMe is recognisable in a NuGet listing instead of showing
  the default placeholder. The build now fails if the icon does not make it into the package, the same way
  it already did for the README. The design is credited in the README, as its licence requires.

## [1.0.0] - 2026-08-07

First stable public release of SignalMe.

1.0.0 means the public CLI contract — commands, accepted values, aliases and exit codes — is now
considered stable. Compatible additions will come in minor versions; anything incompatible would require a
new major version. It is not a claim that the tool has stopped evolving.

### Added

- **Installable as a global .NET tool**: `dotnet tool install --global SignalMe`, then `signalme`.
  Targets .NET 10, runs on Windows.
- **Durable availability statuses**: `available` (alias `free`), `busy`, `away` and `do-not-disturb`
  (alias `dnd`). They stay on until you change them, and are remembered locally.
- **Temporary light signals**: `happy`, `bored`, `desperate`, `warning`, `alerting` and `ready`. Each
  plays an animation and then restores the durable status — including when the animation fails, and when
  it is interrupted. `ready` is the exception: it announces availability and ends on `available`.
- **`signalme status`**, printing the durable status SignalMe last set. It does not open the device —
  Luxafor devices cannot report their LED state back — so it reports what was requested, and works with no
  device connected.
- **`signalme off`**, turning every LED off and forgetting the durable status. `switch-off` is an alias.
- **Ctrl+C** stops a signal, restores the previous durable status, and exits cleanly.
- **Stable exit codes**: `0` success, `1` usage error, `2` device error, `3` unexpected error,
  `4` interrupted.
- **Documentation**: a command reference, the semantics of every status and signal, the tested and
  untested hardware, troubleshooting, and a development and release guide.

### Notes

- A command never reports success for something the device refused. Every device call is checked, and a
  refusal fails the command with a message naming the operation.
- The durable status is stored in `%LOCALAPPDATA%\SignalMe\signalme.ini`, written through a temporary file
  so an interruption cannot leave it half written. A missing or unreadable file reads as "no status".
- SignalMe uses the first Luxafor device the controller library discovers. Explicit device selection is
  not supported in 1.0.
- Only the Luxafor Orb has been tested. See
  [Hardware and platform support](docs/hardware.md) for what is expected to work and what is not
  supported.

[Unreleased]: https://github.com/Reefact/signalme/compare/v2.0.0-preview.2...HEAD
[2.0.0-preview.2]: https://github.com/Reefact/signalme/compare/v2.0.0-preview.1...v2.0.0-preview.2
[2.0.0-preview.1]: https://github.com/Reefact/signalme/compare/v1.0.2...v2.0.0-preview.1
[1.0.2]: https://github.com/Reefact/signalme/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/Reefact/signalme/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Reefact/signalme/releases/tag/v1.0.0
