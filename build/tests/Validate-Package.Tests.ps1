<#
    Validate-Package.ps1 is the last gate before nuget.org, and it only earns that place if it actually
    fails. Each test hands it a package that is wrong in exactly one way and checks it says so.
#>

BeforeAll {
    $script:Validate = (Resolve-Path (Join-Path $PSScriptRoot '..' 'Validate-Package.ps1')).Path
    Import-Module (Join-Path $PSScriptRoot 'PackageFixture.psm1') -Force

    $script:Root = Join-Path ([System.IO.Path]::GetTempPath()) ('signalme-package-tests-' + [Guid]::NewGuid().ToString('N'))

    # Splatted so a test reads as the defect it introduces and nothing else.
    function Invoke-Validation {
        param([hashtable] $Package = @{})

        $artifacts = New-TestPackage -Path (Join-Path $script:Root ([Guid]::NewGuid().ToString('N'))) @Package
        & $script:Validate -ArtifactsDirectory $artifacts
    }
}

AfterAll {
    Remove-Item $script:Root -Recurse -Force -ErrorAction SilentlyContinue
}

Describe 'Validate-Package' {

    Context 'a package with nothing wrong with it' {

        It 'passes' {
            { Invoke-Validation } | Should -Not -Throw
        }

        # Joined into one string: Should -Match asserts against a single value, and the script writes its
        # summary as a separate line from the one naming the package.
        It 'reports the id, the version and the command it found' {
            $output = (Invoke-Validation 6>&1) -join [Environment]::NewLine
            $output | Should -Match 'SignalMe 1\.0\.2'
            $output | Should -Match "command 'signalme'"
        }
    }

    Context "the icon against nuget.org's 1 MB limit" {

        It 'accepts an icon one byte under the limit' {
            { Invoke-Validation @{ IconSize = 1MB - 1 } } | Should -Not -Throw
        }

        # The limit nuget.org documents is a ceiling and the development guide says "under 1 MB", so a file
        # landing exactly on it is refused here. Trimming a byte costs nothing; a rejected push costs a tag.
        It 'rejects an icon of exactly 1 MB' {
            { Invoke-Validation @{ IconSize = 1MB } } | Should -Throw '*stay under 1 MB*'
        }

        It 'rejects an icon over the limit' {
            { Invoke-Validation @{ IconSize = 1MB + 1KB } } | Should -Throw '*stay under 1 MB*'
        }

        It 'reports the actual size, so the gap to the limit is visible' {
            { Invoke-Validation @{ IconSize = 2MB } } | Should -Throw '*2,097,152 bytes*'
        }

        # '-f' and ToString() follow the machine's culture: without the invariant culture this message reads
        # '1 048 576' on a French agent, and every assertion above passes only on an English one.
        It 'formats the size the same way whatever the culture of the agent' {
            $current = [System.Threading.Thread]::CurrentThread.CurrentCulture
            try {
                [System.Threading.Thread]::CurrentThread.CurrentCulture = [cultureinfo]::GetCultureInfo('fr-FR')
                { Invoke-Validation @{ IconSize = 2MB } } | Should -Throw '*2,097,152 bytes*'
            } finally {
                [System.Threading.Thread]::CurrentThread.CurrentCulture = $current
            }
        }
    }

    Context 'the artifacts directory' {

        It 'fails when no package was produced' {
            $empty = Join-Path $script:Root ([Guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Path $empty -Force | Out-Null
            { & $script:Validate -ArtifactsDirectory $empty } | Should -Throw '*No .nupkg found*'
        }

        It 'fails when the symbol package is missing' {
            { Invoke-Validation @{ WithoutSymbols = $true } } | Should -Throw '*No .snupkg found*'
        }
    }

    Context 'the package metadata' {

        It 'fails on an unexpected package id' {
            { Invoke-Validation @{ PackageId = 'SignalMeToo' } } | Should -Throw "*Expected the package id to be 'SignalMe'*"
        }

        It 'fails when the tool marker is absent, which is what a lost <PackAsTool> looks like' {
            { Invoke-Validation @{ PackageType = 'Dependency' } } | Should -Throw '*not marked as a DotnetTool*'
        }

        It 'fails on a licence that is not the project one' {
            { Invoke-Validation @{ License = 'MIT' } } | Should -Throw '*Expected the Apache-2.0 license expression*'
        }

        It 'fails without a description' {
            { Invoke-Validation @{ Description = '' } } | Should -Throw '*no description*'
        }

        It 'fails without a repository URL' {
            { Invoke-Validation @{ RepositoryUrl = '' } } | Should -Throw '*no repository URL*'
        }

        It 'fails when no README is declared' {
            { Invoke-Validation @{ DeclaredReadme = '' } } | Should -Throw '*declares no README*'
        }

        It 'fails when no icon is declared' {
            { Invoke-Validation @{ DeclaredIcon = '' } } | Should -Throw '*declares no icon*'
        }
    }

    Context 'the package content' {

        It 'fails when <_> did not make it in' -ForEach @(
            'README.md',
            'icon.png',
            'tools/net10.0/any/DotnetToolSettings.xml',
            'tools/net10.0/any/SignalMe.dll',
            'tools/net10.0/any/SignalMe.runtimeconfig.json',
            'tools/net10.0/any/Reefact.LuxaforLightingDeviceController.dll',
            'tools/net10.0/any/HidLibrary.dll',
            'tools/net10.0/any/Spectre.Console.Cli.dll',
            'tools/net10.0/any/Microsoft.Win32.SystemEvents.dll'
        ) {
            { Invoke-Validation @{ Omit = @($_) } } | Should -Throw "*missing '$_'*"
        }

        It 'fails when the installed command is not the one users are told to run' {
            { Invoke-Validation @{ CommandName = 'signal-me' } } | Should -Throw "*Expected the installed command to be 'signalme'*"
        }

        It 'fails when source files leaked into the package' {
            { Invoke-Validation @{ WithStraySource = $true } } | Should -Throw '*Unexpected files in the package*'
        }
    }
}
