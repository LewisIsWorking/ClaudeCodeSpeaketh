using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ClaudeCodeSpeaketh.Services;

// Resolves a Python interpreter that actually has edge_tts. A GUI app's PATH
// often hits the Microsoft Store 'WindowsApps\python.exe' stub first (works from
// a shell, fails from the app) -- so we test candidates by importing edge_tts
// and cache the first that succeeds.
internal static class PythonResolver
{
    private static string? _exe;
    public static string Exe => _exe ??= Resolve();

    private static string Resolve()
    {
        foreach (var candidate in Candidates())
            if (CanImportEdgeTts(candidate)) return candidate;
        return "python"; // last resort; caller falls back to SAPI on failure
    }

    private static IEnumerable<string> Candidates()
    {
        foreach (var p in Where("python"))
            if (!p.Contains("WindowsApps", StringComparison.OrdinalIgnoreCase)) yield return p;
        yield return "py";
        yield return "python";
    }

    private static IEnumerable<string> Where(string name)
    {
        try
        {
            var psi = new ProcessStartInfo("where.exe", name)
            { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            if (p is null) return Array.Empty<string>();
            var outp = p.StandardOutput.ReadToEnd();
            p.WaitForExit(4000);
            return outp.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    private static bool CanImportEdgeTts(string exe)
    {
        try
        {
            var psi = new ProcessStartInfo(exe)
            { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add("import edge_tts");
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.StandardError.ReadToEnd();
            return p.WaitForExit(8000) && p.ExitCode == 0;
        }
        catch { return false; }
    }
}
