#requires -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [string]$CertificateThumbprint = $env:RECIPIENT_CONFIRM_CERT_THUMBPRINT,
    [string]$OfficePiaPath,
    [switch]$Publish
)
. (Join-Path $PSScriptRoot 'BuildEnvironment.ps1')
$root = Split-Path $PSScriptRoot -Parent
$build = Get-BuildEnvironment
$pia = Get-OfficePiaPath -ExplicitPath $OfficePiaPath
if (-not $CertificateThumbprint) {
    throw 'Pass -CertificateThumbprint. For local testing first run scripts/new-dev-certificate.ps1; never commit a private key.'
}
$thumbprint = $CertificateThumbprint.Replace(' ', '')
if ($thumbprint -notmatch '^[0-9A-Fa-f]{40}$') { throw 'Invalid certificate thumbprint.' }
$certificate = Get-Item -LiteralPath "Cert:\CurrentUser\My\$thumbprint"
if (-not $certificate.HasPrivateKey -or $certificate.NotAfter -le (Get-Date)) {
    throw 'A non-expired signing certificate with a private key is required in CurrentUser\My.'
}
$arguments = @(
    (Join-Path $root 'RecipientConfirm.sln'), '/nologo', '/m', '/restore', '/t:Build',
    "/p:Configuration=$Configuration", "/p:VisualStudioVersion=$($build.Version)",
    "/p:VSToolsPath=$($build.VSToolsPath)", "/p:OfficePiaPath=$pia",
    '/p:SignManifests=true', "/p:ManifestCertificateThumbprint=$thumbprint"
)
Invoke-CheckedMSBuild -Executable $build.MSBuild -BuildArguments $arguments
foreach ($name in @('RecipientConfirm.dll', 'RecipientConfirm.dll.manifest', 'RecipientConfirm.vsto')) {
    if (-not (Test-Path -LiteralPath (Join-Path $root "src\RecipientConfirm\bin\$Configuration\$name"))) {
        throw "Missing build output: $name"
    }
}
if ($Publish) {
    $publishDirectory = Join-Path $root 'artifacts\publish\'
    $publishArguments = @(
        (Join-Path $root 'src\RecipientConfirm\RecipientConfirm.csproj'), '/nologo', '/t:Publish',
        "/p:Configuration=$Configuration", "/p:VisualStudioVersion=$($build.Version)",
        "/p:VSToolsPath=$($build.VSToolsPath)", "/p:OfficePiaPath=$pia",
        '/p:SignManifests=true', "/p:ManifestCertificateThumbprint=$thumbprint",
        "/p:PublishUrl=$publishDirectory"
    )
    Invoke-CheckedMSBuild -Executable $build.MSBuild -BuildArguments $publishArguments
    if (-not (Test-Path -LiteralPath (Join-Path $publishDirectory 'RecipientConfirm.vsto'))) {
        throw 'Publish did not produce RecipientConfirm.vsto.'
    }
    Write-Information -InformationAction Continue "Published locally: $publishDirectory"
}
