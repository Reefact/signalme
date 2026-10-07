<#
.SYNOPSIS
    Installs the packaged tool and runs the command lines that work without a Luxafor device.

.DESCRIPTION
    Reading the archive proves the files are there; this proves the thing actually runs. Installed to a
    throwaway tool path so the agent's global tools are left alone.

    No Luxafor device is ever needed. The agent has no device and no interactive input either, so
    launching signalme for real must stop at the device discovery with the documented device error, and
    never reach the prompt: a read of the console input here would wait for a line that never comes.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ArtifactsDirectory
)

$ErrorActionPreference = 'Stop'

$package = Get-ChildItem $ArtifactsDirectory -Filter *.nupkg | Select-Object -First 1
if ($null -eq $package) { throw "No .nupkg found in '$ArtifactsDirectory'." }

$version = [System.Text.RegularExpressions.Regex]::Match($package.BaseName, '(?<version>\d+\.\d+\.\d+.*)$').Groups['version'].Value
$toolPath = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString('N'))

Write-Host "Installing SignalMe $version from $($package.Name)"
dotnet tool install SignalMe --tool-path $toolPath --add-source (Resolve-Path $ArtifactsDirectory) --version $version
if ($LASTEXITCODE -ne 0) { throw 'The tool failed to install.' }

try {
    $signalme = Join-Path $toolPath 'signalme'

    function Invoke-SignalMe {
        param([string[]] $Arguments, [int] $ExpectedExitCode, [string] $What)

        $output = & $signalme @Arguments 2>&1 | Out-String
        if ($LASTEXITCODE -ne $ExpectedExitCode) {
            throw "'signalme $($Arguments -join ' ')' exited with $LASTEXITCODE, expected $ExpectedExitCode ($What).`n$output"
        }

        return $output
    }

    # The help and the version are rendered by the command-line library itself; the unit tests only see
    # their exit codes, because that library prints nothing when the output is redirected on Linux. This
    # Windows agent is where their text is checked.
    $help = Invoke-SignalMe -Arguments @('--help') -ExpectedExitCode 0 -What 'help'
    if ($help -notmatch '--mode') { throw "The help does not mention the '--mode' option.`n$help" }

    $printedVersion = Invoke-SignalMe -Arguments @('--version') -ExpectedExitCode 0 -What 'version'
    if ($printedVersion -notmatch [regex]::Escape($version)) { throw "'--version' does not print the version '$version'.`n$printedVersion" }

    # Usage errors are reported before any device is looked for, so they behave the same with or without one.
    $unknownMode = Invoke-SignalMe -Arguments @('--mode', 'not-a-real-mode') -ExpectedExitCode 1 -What 'unknown mode'
    if ($unknownMode -notmatch 'manual') { throw "The unknown-mode error does not list the 'manual' mode.`n$unknownMode" }

    Invoke-SignalMe -Arguments @('--bogus') -ExpectedExitCode 1 -What 'unknown option' | Out-Null

    # No device on a CI agent: this is the documented device error, reported rather than swallowed, and
    # reached before signalme ever reads its input.
    $noDevice = Invoke-SignalMe -Arguments @() -ExpectedExitCode 2 -What 'no device attached'
    if ([string]::IsNullOrWhiteSpace($noDevice)) { throw 'signalme exited with a device error but said nothing.' }

    Write-Host 'The installed tool runs: help, version, usage errors and the no-device path all behave as documented.'
} finally {
    dotnet tool uninstall SignalMe --tool-path $toolPath 2>&1 | Out-Null
    Remove-Item $toolPath -Recurse -Force -ErrorAction SilentlyContinue
}
