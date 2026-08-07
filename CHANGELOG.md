_[Version française](CHANGELOG-FR.md)_

# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[Unreleased]: https://github.com/Reefact/signalme/compare/v1.0.2...HEAD
[1.0.2]: https://github.com/Reefact/signalme/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/Reefact/signalme/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Reefact/signalme/releases/tag/v1.0.0
