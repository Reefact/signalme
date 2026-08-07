<#
.SYNOPSIS
    Installs the packaged tool and runs the commands that work without a Luxafor device.

.DESCRIPTION
    Reading the archive proves the files are there; this proves the thing actually runs. Installed to a
    throwaway tool path so the agent's global tools are left alone.

    No Luxafor device is ever needed. The device commands are expected to fail here with the documented
    device error code, which is exactly what a user without a device plugged in should see.
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

    $help = Invoke-SignalMe -Arguments @('--help') -ExpectedExitCode 0 -What 'help'
    foreach ($command in @('as', 'off', 'status')) {
        if ($help -notmatch $command) { throw "The help does not mention the '$command' command.`n$help" }
    }

    $usage = Invoke-SignalMe -Arguments @('as', 'not-a-real-status') -ExpectedExitCode 1 -What 'usage error'
    foreach ($value in @('available', 'dnd', 'happy', 'ready')) {
        if ($usage -notmatch $value) { throw "The usage error does not list '$value'.`n$usage" }
    }

    # Reads the local status file only, so it works with no device attached.
    Invoke-SignalMe -Arguments @('status') -ExpectedExitCode 0 -What 'status' | Out-Null

    # No device on a CI agent: this is the documented device error, reported rather than swallowed.
    $noDevice = Invoke-SignalMe -Arguments @('off') -ExpectedExitCode 2 -What 'no device attached'
    if ([string]::IsNullOrWhiteSpace($noDevice)) { throw 'signalme exited with a device error but said nothing.' }

    Write-Host 'The installed tool runs: help, usage error, status and the no-device path all behave as documented.'
} finally {
    dotnet tool uninstall SignalMe --tool-path $toolPath 2>&1 | Out-Null
    Remove-Item $toolPath -Recurse -Force -ErrorAction SilentlyContinue
}
