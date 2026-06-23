using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace ClaudeCodeSpeaketh.Services;

// Speaks one utterance by reusing the deployed tts-speaker.ps1 (so engine/voice
// selection matches the hook exactly). Blocking; cancel kills the speaker process.
internal sealed class SpeechRunner
{
    private readonly string _hooksDir;
    public SpeechRunner(string hooksDir) => _hooksDir = hooksDir;

    public void Speak(string text, CancellationToken ct)
    {
        // tts-speaker.ps1 reads the cleaned text from this handoff file.
        var handoff = Path.Combine(Path.GetTempPath(), "coo-claude-tts.txt");
        File.WriteAllText(handoff, text, new UTF8Encoding(false));

        var script = Path.Combine(_hooksDir, "tts-speaker.ps1");
        if (!File.Exists(script)) return;

        var psi = new ProcessStartInfo("powershell")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };
        foreach (var a in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script })
            psi.ArgumentList.Add(a);

        Process? proc = null;
        try { proc = Process.Start(psi); }
        catch { return; }
        if (proc is null) return;

        using var reg = ct.Register(() =>
        {
            try { if (!proc.HasExited) proc.Kill(true); } catch { }
        });
        proc.WaitForExit();
    }
}
