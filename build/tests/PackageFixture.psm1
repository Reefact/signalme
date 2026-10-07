<#
.SYNOPSIS
    Builds synthetic .nupkg files for the build script tests.

.DESCRIPTION
    Validate-Package.ps1 reads an archive, so testing it means having archives to read. Producing them with
    `dotnet pack` would make every test a build, and a healthy build cannot express the cases that matter:
    the point of the suite is packages that are wrong in one specific way.

    Every parameter defaults to what a healthy SignalMe package looks like, so a test names only the defect
    it is about.
#>

Set-StrictMode -Version Latest

# The layout Validate-Package.ps1 expects, kept here so a test can omit exactly one entry by name.
$script:ToolDirectory = 'tools/net10.0/any'
$script:ToolFiles     = @(
    'DotnetToolSettings.xml',
    'SignalMe.dll',
    'SignalMe.runtimeconfig.json',
    'Reefact.LuxaforLightingDeviceController.dll',
    'HidLibrary.dll',
    'Spectre.Console.Cli.dll',
    'Microsoft.Win32.SystemEvents.dll'
)

function New-TestPackage {
    <#
    .SYNOPSIS
        Writes a .nupkg (and its .snupkg) into a fresh directory and returns that directory.

    .PARAMETER IconSize
        Size in bytes of the icon placed in the package. The content is filler: nothing under test reads
        the image, only its length.

    .PARAMETER Omit
        Package-relative paths to leave out, e.g. 'README.md' or 'tools/net10.0/any/HidLibrary.dll'.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)] [string] $Path,
        [int]      $IconSize        = 4KB,
        [string]   $PackageId       = 'SignalMe',
        [string]   $Version         = '1.0.2',
        [string]   $CommandName     = 'signalme',
        [string]   $License         = 'Apache-2.0',
        [string]   $Description     = 'A tiny command-line companion for Luxafor USB LED devices.',
        [string]   $RepositoryUrl   = 'https://github.com/Reefact/signalme.git',
        [string]   $DeclaredReadme  = 'README.md',
        [string]   $DeclaredIcon    = 'icon.png',
        [string]   $PackageType     = 'DotnetTool',
        [string[]] $Omit            = @(),
        [switch]   $WithoutSymbols,
        [switch]   $WithStraySource
    )

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
    $stage = Join-Path $Path 'stage'
    New-Item -ItemType Directory -Path (Join-Path $stage $script:ToolDirectory) -Force | Out-Null

    # An empty element is how a test says "this metadata is missing": passing '' produces <readme />, which
    # is what a project that never set PackageReadmeFile would pack.
    $nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>$PackageId</id>
    <version>$Version</version>
    <authors>Sylvain AURAT</authors>
    <description>$Description</description>
    <license type="expression">$License</license>
    <icon>$DeclaredIcon</icon>
    <readme>$DeclaredReadme</readme>
    <repository type="git" url="$RepositoryUrl" />
    <packageTypes>
      <packageType name="$PackageType" />
    </packageTypes>
  </metadata>
</package>
"@
    Set-Content -Path (Join-Path $stage "$PackageId.nuspec") -Value $nuspec -Encoding UTF8

    $toolSettings = @"
<?xml version="1.0" encoding="utf-8"?>
<DotNetCliTool Version="1">
  <Commands>
    <Command Name="$CommandName" EntryPoint="$PackageId.dll" Runner="dotnet" />
  </Commands>
</DotNetCliTool>
"@

    Add-PackageFile -Stage $stage -Omit $Omit -RelativePath 'README.md' -Content '# SignalMe'
    Add-PackageFile -Stage $stage -Omit $Omit -RelativePath "$script:ToolDirectory/DotnetToolSettings.xml" -Content $toolSettings
    foreach ($file in $script:ToolFiles | Where-Object { $_ -ne 'DotnetToolSettings.xml' }) {
        Add-PackageFile -Stage $stage -Omit $Omit -RelativePath "$script:ToolDirectory/$file" -Content 'assembly placeholder'
    }
    if ($WithStraySource) {
        Add-PackageFile -Stage $stage -Omit $Omit -RelativePath "$script:ToolDirectory/Program.cs" -Content 'namespace SignalMe;'
    }

    if ('icon.png' -notin $Omit) {
        # Written as bytes rather than text: a character is not reliably a byte, and these tests are about
        # exact sizes.
        $icon = [byte[]]::new($IconSize)
        [System.IO.File]::WriteAllBytes((Join-Path $stage 'icon.png'), $icon)
    }

    $package = Join-Path $Path "$PackageId.$Version.nupkg"
    Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $package -Force
    if (-not $WithoutSymbols) {
        Copy-Item $package (Join-Path $Path "$PackageId.$Version.snupkg")
    }
    Remove-Item $stage -Recurse -Force

    return $Path
}

function Add-PackageFile {
    param(
        [Parameter(Mandatory = $true)] [string]   $Stage,
        [Parameter(Mandatory = $true)] [string]   $RelativePath,
        [Parameter(Mandatory = $true)] [string]   $Content,
        [Parameter(Mandatory = $true)] [AllowEmptyCollection()] [string[]] $Omit
    )

    if ($RelativePath -in $Omit) { return }

    $full = Join-Path $Stage ($RelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    New-Item -ItemType Directory -Path (Split-Path $full -Parent) -Force | Out-Null
    Set-Content -Path $full -Value $Content -Encoding UTF8
}

Export-ModuleMember -Function New-TestPackage
