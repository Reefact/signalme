<#
.SYNOPSIS
    Checks that the produced .nupkg really is an installable signalme tool package.

.DESCRIPTION
    Packaging mistakes are silent: a missing tool marker, a README that did not make it in, or a stray
    file only show up once someone tries to install the thing. This reads the archive and fails the build
    instead.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ArtifactsDirectory
)

$ErrorActionPreference = 'Stop'

$package = Get-ChildItem $ArtifactsDirectory -Filter *.nupkg | Select-Object -First 1
if ($null -eq $package) { throw "No .nupkg found in '$ArtifactsDirectory'." }
Write-Host "Validating $($package.Name)"

$symbols = Get-ChildItem $ArtifactsDirectory -Filter *.snupkg | Select-Object -First 1
if ($null -eq $symbols) { throw "No .snupkg found in '$ArtifactsDirectory': symbol publishing is enabled, it should have been produced." }

$extracted = Join-Path ([System.IO.Path]::GetTempPath()) ([System.Guid]::NewGuid().ToString('N'))
Expand-Archive -Path $package.FullName -DestinationPath $extracted -Force
try {
    $nuspecFile = Get-ChildItem $extracted -Filter *.nuspec | Select-Object -First 1
    if ($null -eq $nuspecFile) { throw 'The package contains no .nuspec.' }
    [xml] $nuspec = Get-Content $nuspecFile.FullName
    $metadata = $nuspec.package.metadata

    if ($metadata.id -ne 'SignalMe') { throw "Expected the package id to be 'SignalMe', found '$($metadata.id)'." }
    if ($metadata.packageTypes.packageType.name -ne 'DotnetTool') { throw 'The package is not marked as a DotnetTool: <PackAsTool> is not taking effect.' }
    if ($metadata.license.'#text' -ne 'Apache-2.0') { throw "Expected the Apache-2.0 license expression, found '$($metadata.license.'#text')'." }
    if ([string]::IsNullOrWhiteSpace($metadata.description)) { throw 'The package has no description.' }
    if ([string]::IsNullOrWhiteSpace($metadata.repository.url)) { throw 'The package declares no repository URL.' }
    if ($metadata.readme -ne 'README.md') { throw 'The package declares no README.' }
    if ($metadata.icon -ne 'icon.png') { throw 'The package declares no icon.' }

    $expected = @(
        'README.md',
        'icon.png',
        'tools/net10.0/any/DotnetToolSettings.xml',
        'tools/net10.0/any/SignalMe.dll',
        'tools/net10.0/any/SignalMe.runtimeconfig.json',
        'tools/net10.0/any/Reefact.LuxaforLightingDeviceController.dll',
        'tools/net10.0/any/HidLibrary.dll',
        'tools/net10.0/any/Spectre.Console.Cli.dll'
    )
    foreach ($entry in $expected) {
        $path = Join-Path $extracted ($entry -replace '/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path $path)) { throw "The package is missing '$entry'." }
    }

    [xml] $toolSettings = Get-Content (Join-Path $extracted 'tools/net10.0/any/DotnetToolSettings.xml')
    $commandName = $toolSettings.DotNetCliTool.Commands.Command.Name
    if ($commandName -ne 'signalme') { throw "Expected the installed command to be 'signalme', found '$commandName'." }

    # Nothing from the build tree should have leaked in.
    $unexpected = Get-ChildItem $extracted -Recurse -File |
        Where-Object { $_.Extension -in @('.cs', '.csproj', '.sln', '.user') }
    if ($unexpected) { throw "Unexpected files in the package: $($unexpected.Name -join ', ')" }

    Write-Host "Package content is valid: $($metadata.id) $($metadata.version), command '$commandName'."
} finally {
    Remove-Item $extracted -Recurse -Force -ErrorAction SilentlyContinue
}
