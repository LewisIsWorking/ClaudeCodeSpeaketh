using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.Media;
using Avalonia.Threading;
using ClaudeCodeSpeaketh.Models;
using ClaudeCodeSpeaketh.Views;

namespace ClaudeCodeSpeaketh.Services;

// SAPI + karaoke: speaks via System.Speech and highlights words using its
// SpeakProgress (word-boundary) events -- fully offline, no python/internet.
// Blocks until done/cancelled. Returns false to fall back to plain speak.
[SupportedOSPlatform("windows")]
internal sealed class SapiKaraokePlayer
{
    public bool Play(string text, TtsConfig cfg, CancellationToken ct)
    {
        // Tokenise into words + their character offsets (SpeakProgress reports the
        // char position of each spoken word).
        var matches = Regex.Matches(text, @"\S+");
        if (matches.Count == 0) return false;
        var offsets = matches.Select(m => m.Index).ToArray();
        var words = matches.Select(m => m.Value).ToList();
        var color = ParseColor(cfg.Karaoke.ColorHex);

        KaraokeWindow? win = null;
        Dispatcher.UIThread.Invoke(() =>
        {
            win = new KaraokeWindow();
            win.SetWords(words);
            win.Configure(cfg.Karaoke.FontSize, cfg.Karaoke.Position);
            win.Show();
        });

        var synth = new SpeechSynthesizer();
        var done = new ManualResetEventSlim(false);
        try
        {
            try { synth.SelectVoice(cfg.Sapi.Voice); } catch { }
            synth.Rate = cfg.Sapi.Rate;
            synth.Volume = cfg.Sapi.Volume;
            synth.SpeakProgress += (_, e) =>
            {
                var idx = IndexForCharPos(offsets, e.CharacterPosition);
                if (idx >= 0 && win is not null) Dispatcher.UIThread.Post(() => win.Highlight(idx, color));
            };
            synth.SpeakCompleted += (_, _) => done.Set();

            synth.SpeakAsync(text);
            using (ct.Register(() => { try { synth.SpeakAsyncCancelAll(); } catch { } }))
                done.Wait();
        }
        catch { return false; }
        finally
        {
            synth.Dispose();
            Dispatcher.UIThread.Invoke(() => win?.Close());
        }
        return true;
    }

    private static int IndexForCharPos(int[] offsets, int charPos)
    {
        var idx = -1;
        for (var i = 0; i < offsets.Length; i++)
        {
            if (offsets[i] <= charPos) idx = i;
            else break;
        }
        return idx;
    }

    private static Color ParseColor(string hex)
    {
        try { return Color.Parse(hex); }
        catch { return Color.Parse("#FFD54A"); }
    }
}
