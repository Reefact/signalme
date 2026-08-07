<#
.SYNOPSIS
    Runs the Pester tests covering the scripts in build/.

.DESCRIPTION
    The packaging scripts guard a one-way door: a version published to nuget.org is immutable, and
    Validate-Package.ps1 is what stands between a broken package and that push. A guard nobody tests is a
    guard nobody can trust, so it gets a suite of its own — synthetic packages, one defect each.

    Same entry point locally and in CI.
#>
[CmdletBinding()]
param(
    [switch] $Detailed
)

$ErrorActionPreference = 'Stop'

$pester = Get-Module -ListAvailable Pester |
    Where-Object { $_.Version.Major -ge 5 } |
    Sort-Object Version -Descending |
    Select-Object -First 1
if ($null -eq $pester) {
    throw 'Pester 5 or later is required: Install-Module Pester -MinimumVersion 5.0.0 -Scope CurrentUser -SkipPublisherCheck'
}
Import-Module $pester

$configuration = New-PesterConfiguration
$configuration.Run.Path          = Join-Path $PSScriptRoot 'tests'
$configuration.Run.Throw         = $true   # Without this the run reports failures and still exits 0.
$configuration.Output.Verbosity  = if ($Detailed) { 'Detailed' } else { 'Normal' }

Invoke-Pester -Configuration $configuration
