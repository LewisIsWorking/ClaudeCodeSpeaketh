using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ClaudeCodeSpeaketh.Helpers;

internal sealed record ElevatedResult(bool Started, bool Declined, int ExitCode, string? Error);

// Runs a one-shot ELEVATED process via the "runas" verb (single UAC prompt).
// Output from an elevated child can't be captured cross-session, so callers
// confirm success by re-scanning state (registry / installed voices) afterwards.
internal static class ElevatedRunner
{
    public static ElevatedResult RunPowerShell(string command)
        => Run("powershell", new[]
        {
            "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command
        });

    public static ElevatedResult RunReg(params string[] regArgs)
        => Run("reg.exe", regArgs);

    private static ElevatedResult Run(string fileName, string[] arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true,   // required for the runas verb
            Verb = "runas",
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var a in arguments) psi.ArgumentList.Add(a);

        try
        {
            using var proc = Process.Start(psi);
            if (proc is null) return new ElevatedResult(false, false, -1, "Process did not start.");
            proc.WaitForExit();
            return new ElevatedResult(true, false, proc.ExitCode, null);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // ERROR_CANCELLED: the user declined the UAC prompt.
            return new ElevatedResult(false, true, -1, "Elevation was cancelled.");
        }
        catch (Exception ex)
        {
            return new ElevatedResult(false, false, -1, ex.Message);
        }
    }
}
