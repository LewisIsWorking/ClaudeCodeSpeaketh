using System;
using System.Runtime.Versioning;
using System.Speech.Synthesis;

namespace ClaudeCodeSpeaketh.Services;

// Plays a short sample so the user can audition a voice/rate/volume without
// committing it to config. Uses SpeakAsync so the UI thread never blocks, and
// cancels any in-flight preview before starting a new one.
[SupportedOSPlatform("windows")]
internal sealed class VoicePreviewService : IDisposable
{
    private const string Sample =
        "Hello! This is how Claude Code will sound when it reads its responses aloud.";

    private SpeechSynthesizer? _synth;

    public void PreviewSapi(string voice, int rate, int volume, string? sample = null)
    {
        Stop();
        _synth = new SpeechSynthesizer();
        try { _synth.SelectVoice(voice); } catch { /* keep default voice */ }
        _synth.Rate = Math.Clamp(rate, -10, 10);
        _synth.Volume = Math.Clamp(volume, 0, 100);
        _synth.SpeakAsync(string.IsNullOrWhiteSpace(sample) ? Sample : sample);
    }

    public void Stop()
    {
        if (_synth is null) return;
        try { _synth.SpeakAsyncCancelAll(); } catch { }
        try { _synth.Dispose(); } catch { }
        _synth = null;
    }

    public void Dispose() => Stop();
}
