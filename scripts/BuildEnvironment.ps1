Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-BuildEnvironment {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw 'VSTO requires Windows and the full-framework MSBuild.exe.'
    }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) {
        throw 'Install Visual Studio Build Tools with the VSTO build tools component. See README.md.'
    }
    $installations = @(& $vswhere -products '*' -requires Microsoft.Component.MSBuild -format json -utf8 | ConvertFrom-Json)
    if ($LASTEXITCODE -ne 0) { throw 'vswhere failed.' }
    foreach ($installation in ($installations | Sort-Object installationVersion -Descending)) {
        $major = ([Version]$installation.installationVersion).Major
        $vsTools = Join-Path $installation.installationPath "MSBuild\Microsoft\VisualStudio\v$major.0"
        $officeTargets = Join-Path $vsTools 'OfficeTools\Microsoft.VisualStudio.Tools.Office.targets'
        $msbuild = Join-Path $installation.installationPath 'MSBuild\Current\Bin\MSBuild.exe'
        if ((Test-Path -LiteralPath $officeTargets) -and (Test-Path -LiteralPath $msbuild)) {
            return @{ MSBuild = $msbuild; VSToolsPath = $vsTools; Version = "$major.0" }
        }
    }
    throw 'No MSBuild installation with OfficeTools targets found. Add Microsoft.VisualStudio.Component.TeamOffice.BuildTools.'
}

function Get-OfficePiaPath {
    param([string]$ExplicitPath)
    $candidates = @()
    if ($ExplicitPath) { $candidates += $ExplicitPath }
    foreach ($programFiles in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
        if ($programFiles) {
            $candidates += Join-Path $programFiles 'Microsoft Visual Studio\Shared\Visual Studio Tools for Office\PIA\Office15'
        }
    }
    foreach ($candidate in $candidates) {
        if ((Test-Path -LiteralPath (Join-Path $candidate 'Microsoft.Office.Interop.Outlook.dll')) -and
            (Test-Path -LiteralPath (Join-Path $candidate 'Office.dll'))) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }
    throw 'Office 15 PIAs not found. Install VSTO build tools or pass -OfficePiaPath containing Outlook and Office PIAs.'
}

function Invoke-CheckedMSBuild {
    param([string]$Executable, [string[]]$BuildArguments)
    & $Executable @BuildArguments
    if ($LASTEXITCODE -ne 0) { throw "MSBuild failed with exit code $LASTEXITCODE." }
}
