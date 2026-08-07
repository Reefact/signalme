# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0]

First public release. SignalMe existed before this as a program you had to clone and build; 0.1.0 is the
first version you can install and run.

### Added

- Distributed as a .NET tool: `dotnet tool install --global SignalMe`, then `signalme`.
- `signalme status`, printing the durable status SignalMe last set. It does not open the device — Luxafor
  devices cannot be queried — so it reports what was asked for, and works with the device unplugged.
- `signalme off` as the name for turning the LEDs off. `switch-off` still works as an alias.
- Ctrl+C interrupts an animation, restores the durable status and exits with code `4`.
- Documented exit codes: `0` success, `1` usage error, `2` device error, `3` unexpected error,
  `4` interrupted.

### Changed

- Targets .NET 10, the current LTS.
- Help distinguishes durable statuses from temporary signals, lists the aliases, and states that `ready`
  ends on `available` rather than restoring the previous status.
- The durable status is stored under the user's local application data instead of next to the executable,
  and is written through a temporary file so an interruption cannot leave it half written.

### Fixed

- A command the device refused is no longer reported as a success. Every device call is checked, and a
  refusal fails the command with a message naming the operation.
- An animation always restores the durable status: on success, on failure, and on Ctrl+C. If the restore
  itself fails after a successful animation, the command fails rather than leaving the LEDs on an
  animation colour. A failed restore never masks the error that interrupted the animation.
- Failures are no longer silent. Missing devices, refused commands and unknown statuses each produce a
  message and a distinct exit code; unknown values are rejected before the device is opened, so a typo is
  reported as a typo.
- A corrupted or empty status file reads as "no status" instead of taking down every signal command.

[Unreleased]: https://github.com/Reefact/signalme/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Reefact/signalme/releases/tag/v0.1.0
