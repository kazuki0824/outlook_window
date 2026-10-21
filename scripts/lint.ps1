#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Import-Module PSScriptAnalyzer -RequiredVersion 1.23.0 -ErrorAction Stop
Push-Location $root
try {
    & dotnet format whitespace . --folder --verify-no-changes --verbosity normal
    if ($LASTEXITCODE -ne 0) { throw "C# format check failed with exit code $LASTEXITCODE." }
    $diagnostics = @(Invoke-ScriptAnalyzer -Path $PSScriptRoot -Recurse -Severity Error, Warning)
    if ($diagnostics.Count -gt 0) {
        $diagnostics | Format-Table -AutoSize | Out-Host
        throw "PowerShell lint failed with $($diagnostics.Count) diagnostic(s)."
    }
} finally {
    Pop-Location
}
