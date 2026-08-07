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

## Tests

The suite runs in under a second and needs no hardware.

- **`FakeLuxaforDevice`** implements `ILuxaforDevice`, the interface the controller library already
  exposes, so no wrapper was needed to make SignalMe testable. It records the commands it accepts and can
  be told to refuse them or to throw, which is how the device-failure paths are covered.
- **`IDelay`** is the seam the animations wait through. Production waits for real; the tests return
  immediately. That is what keeps the suite fast, and it is also what makes cancellation testable at all —
  a test cancels at a chosen wait.
- **`UserCurrentStatus`** takes its directory as an argument, so tests use a temporary folder instead of
  the real user profile.

Tests assert the contract, not the frames: none of them pins a specific animation frame.

Test parallelisation is disabled for one reason — several tests redirect `Console.Error` to assert on what
SignalMe reports, which is process-wide state.

## Local tool installation

```shell
dotnet pack -c Release -o artifacts
dotnet tool install --global SignalMe --add-source ./artifacts --version 1.0.0
```

Use `--tool-path ./tmp-tool` instead of `--global` to try it without touching your global tools.

## CI

`.github/workflows/ci.yml`, on pull requests and pushes to `main`, on a Windows agent — the platform
SignalMe ships to. No physical device is ever needed.

```text
restore → format and analyzer checks → build -warnaserror → test → pack
        → validate package content → install the tool and run it → upload the package
```

`dotnet format whitespace` is deliberately not part of it: this codebase aligns consecutive assignments,
which the default style normalises away. `dotnet format style` and `dotnet format analyzers` are both
enforced.

The last two steps matter more than they look. `build/Validate-Package.ps1` reads the `.nupkg` and checks
it really is an installable tool package — the `DotnetTool` marker, the command name, the expected
assemblies, the README, no stray source files. `build/Test-ToolInstall.ps1` then installs it for real and
runs the commands that work without a device, including the no-device path.

## Releasing SignalMe

Releases are published by `.github/workflows/release.yml`, triggered by a version tag.

```shell
# 1. set <Version> in SignalMe/SignalMe.csproj, e.g. 1.0.0
# 2. update CHANGELOG.md, merge everything into main
git tag v1.0.0
git push origin v1.0.0
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
