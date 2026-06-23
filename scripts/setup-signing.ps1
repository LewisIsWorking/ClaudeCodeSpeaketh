# setup-signing.ps1 -- one-time: create a self-signed code-signing certificate for
# ClaudeCodeSpeaketh and TRUST it for the current user (no admin required).
#
# This removes the "Unknown Publisher" warning on THIS machine. It does NOT give
# SmartScreen reputation for copies downloaded on other machines -- that needs an
# EV certificate. Adding to the CurrentUser Root store may show a one-time Windows
# security confirmation; accept it.
#
# pack.ps1 picks up the cert automatically by subject and signs each release.

$ErrorActionPreference = 'Stop'
$subject = 'CN=ClaudeCodeSpeaketh Dev'

$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert |
    Where-Object { $_.Subject -eq $subject } | Select-Object -First 1

if (-not $cert) {
    Write-Host "Creating self-signed code-signing certificate ($subject)..." -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $subject `
        -CertStoreLocation Cert:\CurrentUser\My -KeyUsage DigitalSignature `
        -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(10)
} else {
    Write-Host "Reusing existing certificate." -ForegroundColor Green
}

# Trust for the current user: Root (so the signature chains to a trusted root) and
# TrustedPublisher (so prompts treat it as a known publisher).
foreach ($storeName in 'Root', 'TrustedPublisher') {
    $store = New-Object System.Security.Cryptography.X509Certificates.X509Store($storeName, 'CurrentUser')
    $store.Open('ReadWrite')
    if (-not ($store.Certificates | Where-Object { $_.Thumbprint -eq $cert.Thumbprint })) {
        $store.Add($cert)
        Write-Host "  added to CurrentUser\$storeName" -ForegroundColor Cyan
    }
    $store.Close()
}

Write-Host "`nDone. Thumbprint: $($cert.Thumbprint)" -ForegroundColor Green
Write-Host "Now run: .\scripts\pack.ps1 -Version X.Y.Z  (it will sign automatically)."
