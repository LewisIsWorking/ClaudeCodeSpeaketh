using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using ClaudeCodeSpeaketh.Helpers;

namespace ClaudeCodeSpeaketh.Services;

// Installs the Irish English (en-IE) text-to-speech voice ("Orla") via an elevated
// DISM add-capability, redirecting DISM's stdout to a temp file so the GUI can poll
// a REAL percentage. Falls back to Install-Language if the capability isn't found.
internal sealed partial class IrishVoiceInstallService
{
    public string ProgressFile { get; } =
        Path.Combine(Path.GetTempPath(), "ccs-orla-install.log");

    // Elevated PowerShell: discover the en-IE TTS capability, then run DISM with
    // output redirected to the progress file (DISM prints "NN.N%" to stdout).
    private string BuildCommand() =>
        "$ErrorActionPreference='Stop'; " +
        $"$f='{ProgressFile}'; if (Test-Path $f) {{ Remove-Item $f -Force }}; " +
        "$c = Get-WindowsCapability -Online | " +
        "Where-Object { $_.Name -like 'Language.TextToSpeech*en-IE*' } | Select-Object -First 1; " +
        "if ($c) { & dism.exe /online /add-capability /capabilityname:$($c.Name) /norestart *> $f } " +
        "else { Install-Language en-IE *> $f }";

    /// <summary>Starts the elevated install; null if the user declined the UAC prompt.</summary>
    public Process? Start()
    {
        try { if (File.Exists(ProgressFile)) File.Delete(ProgressFile); } catch { }
        return ElevatedRunner.StartPowerShell(BuildCommand());
    }

    /// <summary>Parses the latest DISM percentage from the progress file (-1 if none yet).</summary>
    public double ReadPercent()
    {
        try
        {
            if (!File.Exists(ProgressFile)) return -1;
            using var fs = new FileStream(ProgressFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            return ParsePercent(sr.ReadToEnd());
        }
        catch { return -1; }
    }

    // Last "NN" or "NN.N" immediately followed by '%'. Internal for testing.
    internal static double ParsePercent(string text)
    {
        var matches = PercentRegex().Matches(text);
        if (matches.Count == 0) return -1;
        var last = matches[^1].Groups[1].Value.Replace(',', '.');
        return double.TryParse(last, System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : -1;
    }

    [GeneratedRegex(@"(\d{1,3}(?:[.,]\d)?)\s*%")]
    private static partial Regex PercentRegex();
}
