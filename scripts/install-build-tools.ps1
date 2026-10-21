#requires -Version 5.1
[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'BuildEnvironment.ps1')
try {
    $null = Get-BuildEnvironment
    $null = Get-OfficePiaPath
    $net48 = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
    if (Test-Path -LiteralPath $net48) {
        Write-Host 'VSTO tools, Office PIAs and .NET Framework 4.8 references are already installed.'
        return
    }
} catch {
    Write-Host 'Installing the required Windows build components.'
}
$bootstrapper = Join-Path ([IO.Path]::GetTempPath()) ('recipient-confirm-buildtools-' + [Guid]::NewGuid() + '.exe')
try {
    Invoke-WebRequest -UseBasicParsing -Uri 'https://aka.ms/vs/17/release/vs_buildtools.exe' -OutFile $bootstrapper
    $signature = Get-AuthenticodeSignature -FilePath $bootstrapper
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') {
        throw 'Build Tools bootstrapper does not have a valid Microsoft signature.'
    }
    $arguments = @('--quiet', '--wait', '--norestart',
        '--add', 'Microsoft.VisualStudio.Workload.MSBuildTools',
        '--add', 'Microsoft.VisualStudio.Component.TeamOffice.BuildTools',
        '--add', 'Microsoft.Component.ClickOnce.MSBuild',
        '--add', 'Microsoft.Net.Component.4.8.SDK',
        '--add', 'Microsoft.Net.Component.4.8.TargetingPack')
    $process = Start-Process -FilePath $bootstrapper -ArgumentList $arguments -Wait -PassThru
    if ($process.ExitCode -notin @(0, 3010)) { throw "Build Tools installation failed: $($process.ExitCode)" }
    if ($process.ExitCode -eq 3010) { Write-Warning 'Windows reports a restart is required.' }
    $null = Get-BuildEnvironment
    $null = Get-OfficePiaPath
} finally {
    Remove-Item -LiteralPath $bootstrapper -ErrorAction SilentlyContinue
}
