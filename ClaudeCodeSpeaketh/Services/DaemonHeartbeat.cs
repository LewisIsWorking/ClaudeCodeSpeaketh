using System;
using System.IO;
using System.Threading;

namespace ClaudeCodeSpeaketh.Services;

// While the resident app runs, refreshes a lock file (PID + unix-seconds) so the
// Stop hook can tell the daemon is alive and enqueue instead of speaking directly.
internal sealed class DaemonHeartbeat : IDisposable
{
    public const int StaleSeconds = 15;
    private const int IntervalMs = 5000;

    private readonly string _lockFile;
    private readonly Timer _timer;

    public DaemonHeartbeat(string hooksDir)
    {
        _lockFile = LockPath(hooksDir);
        Write();
        _timer = new Timer(_ => Write(), null, IntervalMs, IntervalMs);
    }

    private void Write()
    {
        try
        {
            File.WriteAllText(_lockFile,
                Environment.ProcessId + "\n" + DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
        catch { /* best-effort */ }
    }

    public void Dispose()
    {
        _timer.Dispose();
        try { if (File.Exists(_lockFile)) File.Delete(_lockFile); } catch { }
    }

    private static string LockPath(string hooksDir) => Path.Combine(hooksDir, ".tts-daemon.lock");

    public static bool IsAlive(string hooksDir)
    {
        try
        {
            var f = LockPath(hooksDir);
            if (!File.Exists(f)) return false;
            var parts = File.ReadAllText(f).Split('\n');
            return parts.Length >= 2
                && long.TryParse(parts[1].Trim(), out var ts)
                && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts < StaleSeconds;
        }
        catch { return false; }
    }
}
