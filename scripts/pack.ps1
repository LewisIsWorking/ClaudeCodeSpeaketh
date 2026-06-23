# pack.ps1 -- builds a Velopack Windows installer (Setup.exe) for ClaudeCodeSpeaketh.
#
# Requires the 'vpk' global tool whose version matches the Velopack NuGet (1.2.0):
#   dotnet tool install -g vpk --version 1.2.0
#
# Usage:  .\scripts\pack.ps1            # 0.1.0
#         .\scripts\pack.ps1 -Version 0.2.0
param([string]$Version = "0.1.0")

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root 'ClaudeCodeSpeaketh\ClaudeCodeSpeaketh.csproj'
$pub  = Join-Path $root 'publish'
$rel  = Join-Path $root 'Releases'

Write-Host "==> Publishing self-contained win-x64..." -ForegroundColor Cyan
if (Test-Path $pub) { Remove-Item -LiteralPath $pub -Recurse -Force }
dotnet publish $proj -c Release -r win-x64 --self-contained true -o $pub
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Code-signing: if the self-signed cert (see setup-signing.ps1) + signtool exist,
# sign every packaged file. Otherwise pack unsigned (warn) -- never hard-fail.
$signArgs = @()
$cert = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert -ErrorAction SilentlyContinue |
    Where-Object { $_.Subject -eq 'CN=ClaudeCodeSpeaketh Dev' } | Select-Object -First 1
$signtool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe' -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
if ($cert -and $signtool) {
    $tpl = "`"$($signtool.FullName)`" sign /sha1 $($cert.Thumbprint) /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 {{file}}"
    $signArgs = @('--signTemplate', $tpl)
    Write-Host "==> Signing with cert $($cert.Thumbprint)" -ForegroundColor Cyan
} else {
    Write-Host "==> No signing cert/signtool found -- packing UNSIGNED. Run scripts\setup-signing.ps1 to enable." -ForegroundColor Yellow
}

Write-Host "==> Packing with Velopack $Version..." -ForegroundColor Cyan
vpk pack `
    --packId ClaudeCodeSpeaketh `
    --packVersion $Version `
    --packDir $pub `
    --mainExe ClaudeCodeSpeaketh.exe `
    --packTitle "ClaudeCodeSpeaketh" `
    --packAuthors "Lewis" `
    --outputDir $rel `
    @signArgs
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed" }

Write-Host "`n==> Done. Installer(s):" -ForegroundColor Green
Get-ChildItem -LiteralPath $rel -Filter '*Setup*.exe' -ErrorAction SilentlyContinue |
    ForEach-Object { Write-Host ("    " + $_.FullName) -ForegroundColor Green }
