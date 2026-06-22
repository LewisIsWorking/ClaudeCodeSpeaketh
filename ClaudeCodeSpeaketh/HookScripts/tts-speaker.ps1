# tts-speaker.ps1 -- detached speaker launched by speak-response.ps1.
#
# Runs in its own hidden process so synchronous speech doesn't block Claude Code.
# Records its own PID so the next turn can interrupt it. Routes to SAPI or Piper
# based on tts-config.json. PURE ASCII on purpose.

$ErrorActionPreference = 'SilentlyContinue'
$synth = $null
try {
    . (Join-Path $PSScriptRoot 'tts-config.ps1')
    $cfg = Get-TtsConfig

    $pidFile  = Join-Path $PSScriptRoot '.tts-speaker.pid'
    $textFile = Join-Path $env:TEMP 'coo-claude-tts.txt'

    $PID | Set-Content -LiteralPath $pidFile -Encoding ASCII
    if (-not (Test-Path -LiteralPath $textFile)) { return }
    $text = Get-Content -LiteralPath $textFile -Raw -Encoding UTF8
    if ([string]::IsNullOrWhiteSpace($text)) { return }

    # --- Piper route (neural). Falls back to SAPI if unavailable. -------------
    $piperScript = Join-Path $PSScriptRoot 'tts-piper.ps1'
    $usePiper = ($cfg.Engine -eq 'piper') -and
                (Test-Path -LiteralPath $piperScript) -and
                $cfg.PiperExe -and (Test-Path -LiteralPath $cfg.PiperExe) -and
                $cfg.PiperModel -and (Test-Path -LiteralPath $cfg.PiperModel)
    if ($usePiper) {
        . $piperScript
        if (Invoke-PiperSpeak -Text $text -Config $cfg) { return }
        # else fall through to SAPI
    }

    # --- SAPI route (classic System.Speech) -----------------------------------
    Add-Type -AssemblyName System.Speech
    $synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $voice = if ($cfg.SapiVoice) { $cfg.SapiVoice } else { 'Microsoft Hazel Desktop' }
    try { $synth.SelectVoice($voice) } catch {}
    $synth.Rate   = [int]$cfg.Rate
    $synth.Volume = [int]$cfg.Volume
    $synth.Speak($text)   # synchronous, but detached so nothing waits
}
catch { }
finally {
    if ($synth) { $synth.Dispose() }
    $pf = Join-Path $PSScriptRoot '.tts-speaker.pid'
    if (Test-Path -LiteralPath $pf) { Remove-Item -LiteralPath $pf -Force -ErrorAction SilentlyContinue }
}
