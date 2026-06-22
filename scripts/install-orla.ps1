# install-orla.ps1 -- installs the Irish English (en-IE) text-to-speech voice
# ("Microsoft Orla") as a Windows Feature on Demand. Must run ELEVATED.
#
# After this, the voice is registered for OneCore/Narrator. ClaudeCodeSpeaketh's
# OneCore->SAPI mirror (Phase 2) then exposes it to the classic System.Speech
# engine the Claude Code hook uses.

$ErrorActionPreference = 'Stop'

Write-Host "Looking for the English (Ireland) text-to-speech capability..." -ForegroundColor Cyan
$caps = Get-WindowsCapability -Online | Where-Object { $_.Name -like 'Language.TextToSpeech*en-IE*' }

if (-not $caps) {
    Write-Host "No targeted en-IE TTS capability found; installing the full en-IE language instead." -ForegroundColor Yellow
    Install-Language en-IE
} else {
    foreach ($c in $caps) {
        Write-Host ("  {0}  [{1}]" -f $c.Name, $c.State)
        if ($c.State -ne 'Installed') {
            Write-Host "  installing..." -ForegroundColor Cyan
            Add-WindowsCapability -Online -Name $c.Name | Out-Null
        } else {
            Write-Host "  already installed." -ForegroundColor Green
        }
    }
}

Write-Host "`nen-IE / Orla voices now registered under OneCore:" -ForegroundColor Cyan
$found = Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens' -ErrorAction SilentlyContinue |
    Where-Object { $_.PSChildName -match 'enIE|en-IE|Orla' }
if ($found) { $found | ForEach-Object { Write-Host ("  " + $_.PSChildName) -ForegroundColor Green } }
else { Write-Host "  (none yet -- a sign-out/in or reboot may be needed for it to appear)" -ForegroundColor Yellow }

Write-Host "`nDone. Back in Claude Code, tell me it's installed and I'll wire up the mirror." -ForegroundColor Cyan
Read-Host "Press Enter to close"
