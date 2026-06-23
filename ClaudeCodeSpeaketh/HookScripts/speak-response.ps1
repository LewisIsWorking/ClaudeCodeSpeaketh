# speak-response.ps1 -- Claude Code "Stop" hook (config-driven TTS).
#
# Fires when Claude finishes a turn. Extracts the last spoken assistant text from
# the transcript, strips markdown/code, then hands it to a detached speaker
# (tts-speaker.ps1) so Claude Code is never blocked on audio. Behaviour is driven
# by tts-config.json via tts-config.ps1 (GUI: ClaudeCodeSpeaketh).
#
# PURE ASCII on purpose (Windows PowerShell 5.1 ANSI-mangles BOM-less non-ASCII).

# Read $input FIRST, at top-level scope: $input is EMPTY inside try{}, and
# [Console]::In is empty under "powershell -File" with redirected stdin.
$raw = $input | Out-String

$ErrorActionPreference = 'Stop'
try {
    . (Join-Path $PSScriptRoot 'tts-config.ps1')
    $cfg = Get-TtsConfig
    if (-not $cfg.Enabled) { exit 0 }
    if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }

    $data = $raw | ConvertFrom-Json
    $transcript = $data.transcript_path
    if (-not $transcript -or -not (Test-Path -LiteralPath $transcript)) { exit 0 }

    # --- find the last assistant message that has a text block ----------------
    $lines = Get-Content -LiteralPath $transcript
    $text = $null
    for ($i = $lines.Count - 1; $i -ge 0; $i--) {
        $line = $lines[$i]
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '"type":"assistant"') { continue }
        try { $obj = $line | ConvertFrom-Json } catch { continue }
        if ($obj.type -ne 'assistant') { continue }
        $blocks = $obj.message.content
        if (-not $blocks) { continue }
        # Concatenate ONLY text blocks (never 'thinking' or 'tool_use').
        $parts = foreach ($b in $blocks) { if ($b.type -eq 'text') { $b.text } }
        $joined = ($parts -join "`n").Trim()
        if ($joined) { $text = $joined; break }
    }
    if (-not $text) { exit 0 }

    # --- strip markdown / code so the voice speaks prose, not punctuation -----
    if ($cfg.StripMarkdown) {
        $text = [regex]::Replace($text, '(?s)```.*?```', ' ')          # fenced code
        $text = [regex]::Replace($text, '\[([^\]]+)\]\([^)]+\)', '$1')  # links -> label
        $text = [regex]::Replace($text, 'https?://\S+', ' ')           # bare URLs
        $text = $text -replace '[`*_#>]', ' '                          # inline markers
        $text = [regex]::Replace($text, '(?m)^\s*[-+]\s+', ' ')        # list bullets
        $text = [regex]::Replace($text, '[^\x09\x0A\x0D\x20-\x7E]', ' ') # non-ASCII
    }
    $text = [regex]::Replace($text, '\s+', ' ').Trim()
    if (-not $text) { exit 0 }

    # --- cap length only if maxChars > 0 (0 / ALL = speak everything) ---------
    $max = [int]$cfg.MaxChars
    if ($max -gt 0 -and $text.Length -gt $max) {
        $cut = $text.Substring(0, $max)
        $lastStop = [Math]::Max($cut.LastIndexOf('. '), $cut.LastIndexOf('! '))
        $lastStop = [Math]::Max($lastStop, $cut.LastIndexOf('? '))
        if ($lastStop -gt ($max * 0.4)) { $cut = $cut.Substring(0, $lastStop + 1) }
        $text = $cut.TrimEnd() + ' ...'
    }

    # --- if the resident app (daemon) is alive, enqueue; else speak directly ---
    $lock = Join-Path $PSScriptRoot '.tts-daemon.lock'
    $daemonAlive = $false
    if (Test-Path -LiteralPath $lock) {
        $parts = (Get-Content -LiteralPath $lock -Raw) -split "`n"
        if ($parts.Count -ge 2) {
            $ts = $parts[1].Trim() -as [int64]
            $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
            if ($ts -and (($now - $ts) -lt 15)) { $daemonAlive = $true }
        }
    }

    if ($daemonAlive) {
        # Drop a {sessionId, text} file; the resident app serializes playback
        # across sessions and drives the karaoke window.
        $queueDir = Join-Path $PSScriptRoot 'tts-queue'
        New-Item -ItemType Directory -Path $queueDir -Force | Out-Null
        $sid = if ($data.session_id) { [string]$data.session_id } else { 'unknown' }
        $payload = @{ sessionId = $sid; text = $text } | ConvertTo-Json -Compress
        $name = ([DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds().ToString()) + '-' +
                ([guid]::NewGuid().ToString('N').Substring(0, 6)) + '.json'
        Set-Content -LiteralPath (Join-Path $queueDir $name) -Value $payload -Encoding UTF8
        exit 0
    }

    # --- daemon not running: interrupt prior speech + speak directly (fallback) -
    $pidFile  = Join-Path $PSScriptRoot '.tts-speaker.pid'
    $textFile = Join-Path $env:TEMP 'coo-claude-tts.txt'
    if (Test-Path -LiteralPath $pidFile) {
        $oldPid = (Get-Content -LiteralPath $pidFile -ErrorAction SilentlyContinue) -as [int]
        if ($oldPid) { try { Stop-Process -Id $oldPid -Force -ErrorAction Stop } catch {} }
    }
    Set-Content -LiteralPath $textFile -Value $text -Encoding UTF8
    $speaker = Join-Path $PSScriptRoot 'tts-speaker.ps1'
    Start-Process -FilePath 'powershell' `
        -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',"`"$speaker`"" `
        -WindowStyle Hidden | Out-Null
}
catch {
    # A Stop hook must never block Claude Code; swallow everything.
}
exit 0
