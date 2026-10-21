#requires -Version 5.1
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
# Development only. This does not import anything into Trusted Root/Trusted Publishers.
$certificate = New-SelfSignedCertificate -Type CodeSigningCert `
    -Subject 'CN=RecipientConfirm Development' -CertStoreLocation 'Cert:\CurrentUser\My' `
    -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable `
    -Provider 'Microsoft Enhanced RSA and AES Cryptographic Provider' `
    -NotAfter (Get-Date).AddYears(1)
Write-Host 'Development signing certificate created in CurrentUser\My. Trust is not installed.'
# Only the thumbprint is written to the success stream, for assignment in a calling script.
$certificate.Thumbprint
