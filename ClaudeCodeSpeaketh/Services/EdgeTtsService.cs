using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ClaudeCodeSpeaketh.Models;

namespace ClaudeCodeSpeaketh.Services;

// Talks to the edge-tts Python package: availability, pip install, voice list,
// and preview (reuses the deployed tts-edge.ps1 so playback matches the hook).
internal sealed class EdgeTtsService
{
    private readonly string _hooksDir;
    public EdgeTtsService(string hooksDir) => _hooksDir = hooksDir;

    /// <summary>Fast, offline check that python + the edge_tts module import.</summary>
    public bool IsAvailable()
        => Run("python", new[] { "-c", "import edge_tts" }, 8000).code == 0;

    /// <summary>pip-installs edge-tts (network). Returns true on success.</summary>
    public bool Install()
        => Run("python", new[] { "-m", "pip", "install", "--user", "edge-tts" }, 120000).code == 0;

    /// <summary>Fetches the voice catalogue (network) and keeps the English voices.</summary>
    public IReadOnlyList<EdgeVoiceInfo> GetEnglishVoices()
    {
        var list = new List<EdgeVoiceInfo>();
        var (code, output) = Run("python", new[] { "-m", "edge_tts", "--list-voices" }, 30000);
        if (code != 0) return list;

        foreach (var raw in output.Split('\n'))
        {
            var line = raw.Trim();
            if (!line.StartsWith("en-", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;
            var name = parts[0];
            var dash = name.IndexOf('-', 3);
            list.Add(new EdgeVoiceInfo
            {
                ShortName = name,
                Gender = parts[1],
                Locale = dash > 0 ? name.Substring(0, dash) : name,
            });
        }
        return list;
    }

    /// <summary>Previews a voice by running the deployed tts-edge.ps1 (non-blocking).</summary>
    public void Preview(string voice, string sample)
    {
        var script = Path.Combine(_hooksDir, "tts-edge.ps1");
        if (!File.Exists(script)) return;
        var psi = new ProcessStartInfo("powershell")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var a in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script,
                                  "-Voice", voice, "-Text", sample })
            psi.ArgumentList.Add(a);
        try { Process.Start(psi); } catch { /* preview is best-effort */ }
    }

    private static (int code, string output) Run(string file, string[] args, int timeoutMs)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        try
        {
            using var p = Process.Start(psi);
            if (p is null) return (-1, "");
            var output = p.StandardOutput.ReadToEnd();
            if (!p.WaitForExit(timeoutMs)) { try { p.Kill(true); } catch { } return (-1, output); }
            return (p.ExitCode, output);
        }
        catch { return (-1, ""); }
    }
}
