# tts-edge.ps1 -- neural speech via edge-tts (free, keyless Microsoft Edge voices).
#
# Dot-source it (defines Invoke-EdgeSpeak) OR run standalone for preview:
#   powershell -File tts-edge.ps1 -Text "hello" -Voice en-IE-EmilyNeural
#
# Returns $true on success; $false lets the caller fall back to SAPI.
# PURE ASCII on purpose.
param(
    [string]$TextFile,
    [string]$Text,
    [string]$Voice = 'en-IE-EmilyNeural',
    [string]$Rate  = '+0%'
)

# Resolve a python that actually has edge_tts, skipping the Microsoft Store stub
# (WindowsApps\python.exe) which a non-shell launcher often hits first.
function Get-Python {
    $cands = @()
    try { $cands += (& where.exe python 2>$null) | Where-Object { $_ -and ($_ -notmatch 'WindowsApps') } } catch {}
    $cands += @('py', 'python')
    foreach ($c in $cands) {
        try { & $c -c "import edge_tts" 2>$null; if ($LASTEXITCODE -eq 0) { return $c } } catch {}
    }
    return 'python'
}

function Invoke-EdgeSpeak {
    param([string]$TextFile, [string]$Text, [string]$Voice, [string]$Rate)

    $mp3 = Join-Path $env:TEMP ("ccs-edge-" + $PID + ".mp3")
    if (Test-Path -LiteralPath $mp3) { Remove-Item -LiteralPath $mp3 -Force -ErrorAction SilentlyContinue }

    $genArgs = @('-m', 'edge_tts', '-v', $Voice, '--rate', $Rate, '--write-media', $mp3)
    if ($TextFile -and (Test-Path -LiteralPath $TextFile)) { $genArgs += @('-f', $TextFile) }
    elseif ($Text) { $genArgs += @('-t', $Text) }
    else { return $false }

    # Generate (needs python + edge_tts + internet). Any failure -> fall back.
    $py = Get-Python
    try { & $py @genArgs 2>$null } catch { return $false }
    if ($LASTEXITCODE -ne 0) { return $false }
    if (-not (Test-Path -LiteralPath $mp3) -or (Get-Item -LiteralPath $mp3).Length -lt 64) { return $false }

    # Play the mp3 headless via WPF MediaPlayer (poll duration, then sleep).
    try {
        Add-Type -AssemblyName presentationCore
        $player = New-Object System.Windows.Media.MediaPlayer
        $player.Open([Uri]::new($mp3))
        $n = 0
        while (-not $player.NaturalDuration.HasTimeSpan -and $n -lt 60) { Start-Sleep -Milliseconds 50; $n++ }
        $secs = if ($player.NaturalDuration.HasTimeSpan) { $player.NaturalDuration.TimeSpan.TotalSeconds } else { 0 }
        $player.Play()
        if ($secs -gt 0) { Start-Sleep -Milliseconds ([int](($secs + 0.4) * 1000)) }
        $player.Close()
        return $true
    } catch { return $false }
    finally { Remove-Item -LiteralPath $mp3 -Force -ErrorAction SilentlyContinue }
}

# Standalone invocation (preview): run if any text source was passed as a param.
if ($Text -or $TextFile) {
    $ok = Invoke-EdgeSpeak -TextFile $TextFile -Text $Text -Voice $Voice -Rate $Rate
    if (-not $ok) { exit 1 }
}
