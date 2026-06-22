# tts-config.ps1 -- shared config loader for the Claude Code TTS hook.
#
# Dot-sourced by speak-response.ps1 and tts-speaker.ps1. Reads tts-config.json
# (written by the ClaudeCodeSpeaketh GUI) sitting next to this script, falls back
# to sensible defaults if it is missing or corrupt, then applies optional
# environment-variable overrides for back-compat. Returns a FLAT hashtable so
# callers never touch the JSON shape.
#
# PURE ASCII on purpose: Windows PowerShell 5.1 reads BOM-less files as the ANSI
# codepage and would corrupt non-ASCII literals.

function Get-TtsConfigPath {
    $dir = Split-Path -Parent $PSCommandPath
    if (-not $dir) { $dir = Split-Path -Parent $MyInvocation.MyCommand.Path }
    Join-Path $dir 'tts-config.json'
}

function Get-Prop {
    param($Obj, [string]$Name, $Default)
    if ($null -eq $Obj) { return $Default }
    $p = $Obj.PSObject.Properties[$Name]
    if ($null -eq $p -or $null -eq $p.Value) { return $Default }
    return $p.Value
}

function Get-TtsConfig {
    # Defaults reproduce the original behaviour, EXCEPT maxChars now defaults to
    # 0 (= ALL / speak the entire response) per the product decision.
    $cfg = @{
        Enabled        = $true
        Engine         = 'sapi'
        SapiVoice      = 'Microsoft Hazel Desktop'
        Rate           = 0
        Volume         = 100
        PiperExe       = ''
        PiperModel     = ''
        PiperLengthScale = 1.0
        PiperUseCuda   = $true
        MaxChars       = 0
        StripMarkdown  = $true
    }

    $path = Get-TtsConfigPath
    if (Test-Path -LiteralPath $path) {
        try {
            $j = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
            $cfg.Enabled       = [bool](Get-Prop $j 'enabled' $cfg.Enabled)
            $cfg.Engine        = [string](Get-Prop $j 'engine' $cfg.Engine)
            $cfg.MaxChars      = [int](Get-Prop $j 'maxChars' $cfg.MaxChars)
            $cfg.StripMarkdown = [bool](Get-Prop $j 'stripMarkdown' $cfg.StripMarkdown)

            $sapi = Get-Prop $j 'sapi' $null
            $cfg.SapiVoice = [string](Get-Prop $sapi 'voice'  $cfg.SapiVoice)
            $cfg.Rate      = [int](Get-Prop $sapi 'rate'      $cfg.Rate)
            $cfg.Volume    = [int](Get-Prop $sapi 'volume'    $cfg.Volume)

            $piper = Get-Prop $j 'piper' $null
            $cfg.PiperExe         = [string](Get-Prop $piper 'exePath'     $cfg.PiperExe)
            $cfg.PiperModel       = [string](Get-Prop $piper 'modelPath'   $cfg.PiperModel)
            $cfg.PiperLengthScale = [double](Get-Prop $piper 'lengthScale' $cfg.PiperLengthScale)
            $cfg.PiperUseCuda     = [bool](Get-Prop $piper 'useCuda'       $cfg.PiperUseCuda)
        } catch {
            # Corrupt file -> keep defaults.
        }
    }

    # --- environment-variable overrides (back-compat) -------------------------
    if ($env:CLAUDE_TTS -eq 'off')            { $cfg.Enabled   = $false }
    if ($env:CLAUDE_TTS_VOICE)                { $cfg.SapiVoice = $env:CLAUDE_TTS_VOICE }
    if ($env:CLAUDE_TTS_RATE -and ($env:CLAUDE_TTS_RATE -as [int]) -ne $null) {
        $cfg.Rate = [int]$env:CLAUDE_TTS_RATE
    }
    if ($env:CLAUDE_TTS_MAXCHARS -and ($env:CLAUDE_TTS_MAXCHARS -as [int]) -ne $null) {
        $cfg.MaxChars = [int]$env:CLAUDE_TTS_MAXCHARS
    }

    return $cfg
}
