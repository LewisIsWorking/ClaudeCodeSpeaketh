# release.ps1 -- one-command release for ClaudeCodeSpeaketh.
#
# Chains the three manual steps into one: pull the latest published release (so
# Velopack can build a correct delta), pack + sign this version, then upload &
# publish it to GitHub. The in-app updater picks it up from there.
#
# Usage:
#   .\scripts\release.ps1 -Version 0.18.0
#   .\scripts\release.ps1 -Version 0.18.0 -SkipPush     # don't git push first
#
# Requires: vpk 1.2.0 (matches the Velopack NuGet) and gh (authenticated).
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [switch]$SkipPush
)

$ErrorActionPreference = 'Stop'
$root    = Split-Path -Parent $PSScriptRoot
$rel     = Join-Path $root 'Releases'
$repoUrl = 'https://github.com/LewisIsWorking/ClaudeCodeSpeaketh'

# --- sanity: tools present ---------------------------------------------------
if (-not (Get-Command vpk -ErrorAction SilentlyContinue)) {
    throw "vpk not found. Install: dotnet tool install -g vpk --version 1.2.0"
}
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "gh (GitHub CLI) not found / not on PATH."
}
$token = gh auth token
if ([string]::IsNullOrWhiteSpace($token)) { throw "gh is not authenticated (gh auth login)." }

# --- guard: don't re-release an existing tag ---------------------------------
$existing = gh release view $Version --repo $repoUrl 2>$null
if ($LASTEXITCODE -eq 0) { throw "Release '$Version' already exists on GitHub. Pick a new version." }

# --- 0. push the branch (so the release commit is on the remote) -------------
if (-not $SkipPush) {
    Write-Host "==> Pushing current branch..." -ForegroundColor Cyan
    git -C $root push
    if ($LASTEXITCODE -ne 0) { throw "git push failed." }
}

# --- 1. pull the latest published release (delta base) -----------------------
Write-Host "==> Downloading latest release for delta base..." -ForegroundColor Cyan
vpk download github --repoUrl $repoUrl --token $token --outputDir $rel
if ($LASTEXITCODE -ne 0) { throw "vpk download failed." }

# --- 2. pack + sign this version --------------------------------------------
& (Join-Path $PSScriptRoot 'pack.ps1') -Version $Version

# --- 3. upload + publish -----------------------------------------------------
Write-Host "==> Uploading + publishing $Version..." -ForegroundColor Cyan
vpk upload github --repoUrl $repoUrl --publish true `
    --releaseName "ClaudeCodeSpeaketh $Version" --tag $Version `
    --token $token --outputDir $rel
if ($LASTEXITCODE -ne 0) { throw "vpk upload failed." }

Write-Host "`n==> Released $Version -> $repoUrl/releases/tag/$Version" -ForegroundColor Green
