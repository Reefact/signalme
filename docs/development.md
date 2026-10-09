_[Version française](development-FR.md)_

# Development

## Requirements

The .NET SDK version pinned in [`global.json`](../global.json).

SignalMe builds with warnings as errors, and which diagnostics exist is decided by the SDK's analyzers and
its compiler. An unpinned SDK means a build that is green on one machine and red on another — which is
exactly what happened once here, a `10.0.110` workstation against a `10.0.302` agent. So the SDK is
pinned to a feature band, patches roll forward within it, and the CI installs it through
`global-json-file` rather than resolving a floating version.

If `dotnet` reports a missing SDK, install the one `global.json` asks for rather than editing the file.
Bumping it is a deliberate change: a new feature band can surface new warnings, which are errors here.

The analyzer package is pinned too, in [`Directory.Build.props`](../Directory.Build.props), alongside the
language and quality settings shared by both projects.

## Build

```shell
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release -o artifacts
```

## How it fits together

SignalMe 2.0 is a resident process with one rule at its centre: **one component writes to the device.**

- A **mode** (`Modes/`) is a source of intents — set a durable status, play a signal, turn off. The manual
  mode reads them from the console; a future mode could get them from a presence service. A mode knows
  neither the device nor the colours.
- The **session monitor** (`Sessions/`) reports the lock and unlock of the Windows session, from the
  thread Windows delivers them on.
- The **status coordinator** (`Runtime/StatusCoordinator.cs`) takes both through a single channel and
  handles them one at a time. It computes the effective status (off, then away while locked, then a
  signal, then the desired status), writes to the device, persists the desired status once the device
  obeyed, and prints what changed. Animations are child tasks of its loop, started and awaited by it, so
  a frame can never land after a status write.
- The **runtime** (`Runtime/SignalMeRuntime.cs`) owns the lifecycle: it starts the monitor, renders the
  initial status before the mode starts, runs the coordinator and the mode side by side, and turns the
  device off and disposes it whatever happened.
- The **command line** (`Commands/`) parses `--mode`, discovers and selects the device, then hands it to
  the runtime and maps its outcome to an exit code.

## Tests

The suite runs in a few seconds and needs no hardware. Everything the real program touches is behind a
seam the tests replace:

- **`FakeLuxaforDevice`** implements `ILuxaforDevice`, the interface the controller library already
  exposes, so no wrapper was needed. It records the commands it accepts and can be told to refuse them
  or to throw, which is how the device-failure paths are covered; its `Path` is settable, so the
  selection tests can tell two devices apart, and so is `IsConnected`, so a test can unplug it.
- **`IConsole`** / **`FakeConsole`**: every console interaction goes through `IConsole`. The fake answers
  reads from a script, then reports the end of input — or leaves a read pending until the token is
  cancelled, like a user who never presses Enter before Ctrl+C. It captures the output, the prompts and
  the errors, and keeps a transcript of reads and writes in the order they happened, which is how a test
  checks that the initial status was printed before the mode's first prompt.
- **`ISessionMonitor`** / **`FakeSessionMonitor`**: the test raises a lock or an unlock by hand, from any
  thread, and can make `Start()` flip the state, raise it, or throw — the three things the real monitor
  does when it closes the gap between construction and subscription.
- **`IDeviceConnectionMonitor`** / **`FakeDeviceConnectionMonitor`**: the test reports the device
  unplugged by hand, at the moment it chooses. The real monitor is tested on its own, over a check the
  test controls and an interval of a few milliseconds.
- **`ILuxaforDeviceDiscovery`** / **`FakeDiscovery`**, returning a list of fakes or throwing. The
  command-line tests run the real command line, `--help` to the exit code, through a `SignalMeServices`
  bag of fakes.
- **`IDelay`** is the seam the animations wait through. Production waits for real; `InstantDelay` returns
  at once, and `ControllableDelay` holds one chosen wait, so a test gets an animation that is genuinely
  running when a lock, another intent or the shutdown arrives. That is what makes the interruption rules
  testable at all.
- **`IMoodPattern`**: an animation is a pure function of the device, the delay and the status it starts
  from. It persists nothing and restores nothing, so the coordinator's rules can be tested without caring
  which frames a pattern sends.
- **`UserCurrentStatus`** takes its directory as an argument, so tests use a temporary folder instead of
  the real user profile.

Tests assert the contract, not the frames: none of them pins a specific animation frame. The messages
SignalMe prints are part of that contract and are asserted exactly.

Assertions are written with [NFluent](https://www.n-fluent.net/), as in the Luxafor library:
`Check.That(actual).IsEqualTo(expected)`, `Check.ThatCode(...).Throws<T>()`. xUnit runs the tests, its
`Assert` is not used. One case stays outside `Check.ThatCode`: NFluent waits on asynchronous code, and a
cancelled task that is waited on reports a generic `TaskCanceledException` instead of the exception that
carries the reason. The few tests that check that reason capture it with xUnit's `Record.ExceptionAsync`
and check it with NFluent.

Test parallelisation is disabled for one reason — a few tests redirect the process console to assert on
what SignalMe reports, which is process-wide state.

### What the tests cannot cover

`WindowsSessionMonitor` subscribes to the session switch notifications of Windows, which no test can
raise. It has no unit test; the coordinator and the runtime are tested against the fake monitor, so what
is left to check by hand is only that the real one delivers. After installing a build (below), with a
device plugged in:

1. run `signalme`, type `busy`: the device is yellow;
2. lock the session (Win+L): the device turns purple;
3. unlock it: the device is yellow again, and the console shows `Windows session locked.` /
   `Effective status: away`, then `Windows session unlocked.` / `Effective status: busy`;
4. `status` reports `Session: active`.

Worth doing once per release, and after any change to the monitor or to the `Microsoft.Win32.SystemEvents`
package it relies on.

### The build scripts

`build/Validate-Package.ps1` guards a one-way door — a version published to nuget.org is immutable — so it
has a suite of its own, in Pester, under `build/tests`.

```shell
./build/Test-BuildScripts.ps1            # -Detailed lists every test
```

It needs Pester 5 (`Install-Module Pester -MinimumVersion 5.0.0 -Scope CurrentUser -SkipPublisherCheck`);
the Windows CI image already ships it. `PackageFixture.psm1` builds synthetic `.nupkg` files rather than
running `dotnet pack`, because a healthy build cannot express what the suite is about: packages that are
wrong in exactly one way. Each test names its defect — an icon of exactly 1 MB, a missing assembly, a
command renamed — and checks the validator rejects it.

## Local tool installation

```shell
dotnet pack -c Release -o artifacts
dotnet tool install --global SignalMe --add-source ./artifacts --version 2.0.0-preview.1
```

Use `--tool-path ./tmp-tool` instead of `--global` to try it without touching your global tools.

## Assets

`assets/icon.png` is the package icon: a glowing orb, 512 × 512, transparent background. It is committed
as it was authored — nothing in the build derives or rewrites it, so replacing the icon means replacing
that file.

One hard constraint on a replacement: **it must stay under 1 MB.** nuget.org rejects a heavier icon, and
it does so at publish time, long after CI has gone green. `build/Validate-Package.ps1` measures the packed
icon against that limit, so a replacement that is too heavy fails the build rather than the release.

## CI

`.github/workflows/ci.yml`, on pull requests and pushes to `main`, on a Windows agent — the platform
SignalMe ships to. No physical device is ever needed.

```text
restore → format and analyzer checks → build -warnaserror → test → pack
        → test the build scripts → validate package content → install the tool and run it
        → upload the package
```

`dotnet format whitespace` is deliberately not part of it: this codebase aligns consecutive assignments,
which the default style normalises away. `dotnet format style` and `dotnet format analyzers` are both
enforced.

The packaging steps matter more than they look. `build/Validate-Package.ps1` reads the `.nupkg` and checks
it really is an installable tool package — the `DotnetTool` marker, the command name, the expected
assemblies, the README, an icon within nuget.org's 1 MB limit, no stray source files.
`build/Test-ToolInstall.ps1` then installs it for real and runs what works without a device: `--help`
must mention `--mode`, `--version` must print the version, an unknown mode and an unknown option must be
usage errors (exit `1`, the first one naming `manual`), and `signalme` with no arguments must exit `2`
with a message — the agent has no device and no interactive input, and SignalMe must say so rather than
wait for a line that will never come. The step before them runs the validator's own tests, so a validator
that stopped rejecting anything is caught before it waves a broken package through.

## Releasing SignalMe

Releases are published by `.github/workflows/release.yml`, triggered by a version tag.

```shell
# 1. set <Version> in SignalMe/SignalMe.csproj, e.g. 2.0.0
# 2. update CHANGELOG.md, merge everything into main
git tag v2.0.0
git push origin v2.0.0
```

The job refuses to publish when the tag does not match the project version, so a mistyped tag cannot
publish the wrong one. It then runs the tests, the package validation and a real tool installation before
pushing anything.

### One-time setup

Publishing uses nuget.org **trusted publishing** (OIDC): no long-lived API key is stored in the
repository. The job exchanges a GitHub OIDC token for a short-lived key at push time. It needs, once:

- On nuget.org, a trusted publishing policy for this repository:

  | Field | Value |
  | --- | --- |
  | Repository owner | `Reefact` |
  | Repository | `signalme` |
  | Workflow file | `release.yml` — the file name alone, **not** `.github/workflows/release.yml` |
  | Environment | `nuget` (optional, matches the job) |

  A policy can be owned by a user or by an organization. There is nothing to configure for the tag:
  nuget.org filters on those fields, and restricting the workflow to `v*` tags is the job of its own
  trigger.

- A `NUGET_USER` secret or repository variable, holding the nuget.org account — user or organization —
  that owns the policy. It is an account name, not a credential.

- A `nuget` environment in the repository settings. Adding a required reviewer on it makes every release
  manually approved.
