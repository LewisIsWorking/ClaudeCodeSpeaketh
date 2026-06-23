using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Avalonia.Media;
using Avalonia.Threading;
using NAudio.Wave;
using ClaudeCodeSpeaketh.Models;
using ClaudeCodeSpeaketh.Views;

namespace ClaudeCodeSpeaketh.Services;

// edge engine + karaoke: synthesizes to mp3 + per-word timings (edge-karaoke.py),
// shows the companion window, plays the mp3 in-process (NAudio) and highlights the
// current word. Blocks until done/cancelled. Returns false to fall back to SAPI.
internal sealed class EdgeKaraokePlayer
{
    private record Word(long Offset, long Duration, string Text);

    private readonly string _hooksDir;
    public EdgeKaraokePlayer(string hooksDir) => _hooksDir = hooksDir;

    // Records why karaoke synthesis failed (e.g. no python/internet) for diagnosis.
    private void LogError(string msg)
    {
        try { File.WriteAllText(Path.Combine(_hooksDir, "ccs-karaoke-error.log"), msg); } catch { }
    }

    public bool Play(string text, TtsConfig cfg, CancellationToken ct)
    {
        var tmp = Path.GetTempPath();
        var mp3 = Path.Combine(tmp, "ccs-karaoke.mp3");
        var json = Path.Combine(tmp, "ccs-karaoke.json");
        var txt = Path.Combine(tmp, "ccs-karaoke.txt");
        File.WriteAllText(txt, text, new UTF8Encoding(false));

        var words = Synthesize(cfg, txt, mp3, json);
        if (words.Count == 0 || !File.Exists(mp3)) return false;

        var color = ParseColor(cfg.Karaoke.ColorHex);
        KaraokeWindow? win = null;
        Dispatcher.UIThread.Invoke(() =>
        {
            win = new KaraokeWindow();
            win.SetWords(words.Select(w => w.Text).ToList());
            win.Configure(cfg.Karaoke.FontSize, cfg.Karaoke.Position);
            win.Show();
        });

        try { PlayAndHighlight(mp3, words, color, win!, ct); }
        catch { return false; }
        finally { Dispatcher.UIThread.Invoke(() => win?.Close()); }
        return true;
    }

    private List<Word> Synthesize(TtsConfig cfg, string txt, string mp3, string json)
    {
        try { File.Delete(json); } catch { }
        var rate = (cfg.Sapi.Rate * 10); // reuse the shared speed slider
        var rateStr = (rate >= 0 ? "+" : "") + rate + "%";
        var script = Path.Combine(_hooksDir, "edge-karaoke.py");

        var psi = new ProcessStartInfo(PythonResolver.Exe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,   // give the child valid stdio handles
            RedirectStandardError = true,    // (a windowless parent otherwise has none)
        };
        foreach (var a in new[] { script, cfg.Edge.Voice, txt, mp3, json, rateStr }) psi.ArgumentList.Add(a);
        try
        {
            using var p = Process.Start(psi);
            if (p is null) return new();
            var err = p.StandardError.ReadToEnd();
            p.StandardOutput.ReadToEnd();
            if (!p.WaitForExit(60000) || p.ExitCode != 0)
            {
                LogError($"{PythonResolver.Exe}\nexit={p.ExitCode}\n{err}");
                return new();
            }
        }
        catch (Exception ex)
        {
            LogError("synth exception: " + ex);
            return new();
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(json));
            return doc.RootElement.EnumerateArray()
                .Select(e => new Word(e.GetProperty("o").GetInt64(),
                                      e.GetProperty("d").GetInt64(),
                                      e.GetProperty("t").GetString() ?? ""))
                .ToList();
        }
        catch { return new(); }
    }

    private static void PlayAndHighlight(string mp3, List<Word> words, Color color, KaraokeWindow win, CancellationToken ct)
    {
        using var reader = new MediaFoundationReader(mp3);
        using var output = new WaveOutEvent();
        output.Init(reader);
        output.Play();

        while (output.PlaybackState == PlaybackState.Playing)
        {
            if (ct.IsCancellationRequested) { output.Stop(); break; }
            var pos = reader.CurrentTime.TotalMilliseconds;
            var idx = IndexForPosition(words, pos);
            if (idx >= 0) Dispatcher.UIThread.Post(() => win.Highlight(idx, color));
            Thread.Sleep(40);
        }
    }

    // Last word whose start offset has been reached.
    private static int IndexForPosition(List<Word> words, double posMs)
    {
        var idx = -1;
        for (var i = 0; i < words.Count; i++)
        {
            if (words[i].Offset <= posMs) idx = i;
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
