#requires -Version 5.1
[CmdletBinding()]
param(
    [string]$Manifest = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts\publish\RecipientConfirm.vsto'),
    [switch]$Uninstall
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (Get-Process -Name OUTLOOK -ErrorAction SilentlyContinue) {
    throw 'Close classic Outlook before installing or uninstalling.'
}
$manifestPath = (Resolve-Path -LiteralPath $Manifest).Path
$candidates = @()
foreach ($commonFiles in @($env:CommonProgramFiles, ${env:CommonProgramFiles(x86)})) {
    if ($commonFiles) { $candidates += Join-Path $commonFiles 'Microsoft Shared\VSTO\10.0\VSTOInstaller.exe' }
}
$installer = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $installer) { throw 'Install Microsoft Visual Studio Tools for Office Runtime first.' }
$operation = if ($Uninstall) { '/Uninstall' } else { '/Install' }
# Keep VSTO trust prompts visible. Do not change Trust Center policy or certificate trust.
$process = Start-Process -FilePath $installer -ArgumentList @($operation, ('"' + $manifestPath + '"')) -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "VSTOInstaller failed with exit code $($process.ExitCode)." }
