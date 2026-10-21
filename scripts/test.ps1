#requires -Version 5.1
[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
. (Join-Path $PSScriptRoot 'BuildEnvironment.ps1')
$root = Split-Path $PSScriptRoot -Parent
$build = Get-BuildEnvironment
Invoke-CheckedMSBuild -Executable $build.MSBuild -BuildArguments @(
    (Join-Path $root 'tests\RecipientConfirm.Tests\RecipientConfirm.Tests.csproj'),
    '/nologo', '/restore', '/t:Build', "/p:Configuration=$Configuration"
)
& (Join-Path $root "tests\RecipientConfirm.Tests\bin\$Configuration\RecipientConfirm.Tests.exe")
if ($LASTEXITCODE -ne 0) { throw "Tests failed with exit code $LASTEXITCODE." }
