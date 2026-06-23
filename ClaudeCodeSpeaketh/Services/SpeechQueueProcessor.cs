using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClaudeCodeSpeaketh.Models;

namespace ClaudeCodeSpeaketh.Services;

// Watches the queue folder and speaks utterances FIFO across sessions, keeping
// only the latest per session, and interrupting the current one if a newer
// utterance from the SAME session arrives. Honours master + per-session enable.
internal sealed class SpeechQueueProcessor : IDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly string _queueDir;
    private readonly Func<TtsConfig> _config;
    private readonly Action<string>? _onSessionSeen;
    private readonly SpeechRunner _runner;
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _stop = new();

    private FileSystemWatcher? _watcher;
    private volatile string? _currentSession;
    private CancellationTokenSource? _currentCts;

    public SpeechQueueProcessor(string hooksDir, Func<TtsConfig> config, Action<string>? onSessionSeen = null)
    {
        _queueDir = Path.Combine(hooksDir, "tts-queue");
        Directory.CreateDirectory(_queueDir);
        _config = config;
        _onSessionSeen = onSessionSeen;
        _runner = new SpeechRunner(hooksDir);
    }

    public void Start()
    {
        _watcher = new FileSystemWatcher(_queueDir, "*.json") { EnableRaisingEvents = true };
        _watcher.Created += OnCreated;
        Task.Run(ProcessLoop);
        _signal.Release(); // process anything already queued
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        var item = ReadItem(e.FullPath);
        if (item is not null)
        {
            _onSessionSeen?.Invoke(item.SessionId);
            // Newer utterance from the session currently speaking -> interrupt it.
            if (item.SessionId == _currentSession) _currentCts?.Cancel();
        }
        _signal.Release();
    }

    private async Task ProcessLoop()
    {
        while (!_stop.IsCancellationRequested)
        {
            await _signal.WaitAsync(1000);
            try { Drain(); } catch { /* keep the loop alive */ }
        }
    }

    private void Drain()
    {
        while (!_stop.IsCancellationRequested)
        {
            DropSupersededPerSession();
            var files = Directory.GetFiles(_queueDir, "*.json").OrderBy(f => f).ToList();
            if (files.Count == 0) return;

            var next = files[0];
            var item = ReadItem(next);
            TryDelete(next);
            if (item is null) continue;

            var cfg = _config();
            if (!cfg.Enabled) continue;                       // master off -> drop
            if (cfg.SessionOverrides.TryGetValue(item.SessionId, out var on) && !on) continue; // muted session

            _currentSession = item.SessionId;
            _currentCts = new CancellationTokenSource();
            try { _runner.Speak(item.Text, _currentCts.Token); } catch { }
            finally { _currentSession = null; _currentCts.Dispose(); _currentCts = null; }
        }
    }

    // Within each session, keep only the newest queued file; delete the rest.
    private void DropSupersededPerSession()
    {
        var entries = Directory.GetFiles(_queueDir, "*.json").OrderBy(f => f)
            .Select(f => (file: f, item: ReadItem(f)))
            .Where(x => x.item is not null)
            .GroupBy(x => x.item!.SessionId);
        foreach (var g in entries)
        {
            var ordered = g.ToList();
            for (var i = 0; i < ordered.Count - 1; i++) TryDelete(ordered[i].file);
        }
    }

    private static QueueItem? ReadItem(string path)
    {
        try { return JsonSerializer.Deserialize<QueueItem>(File.ReadAllText(path), Json); }
        catch { return null; }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _currentCts?.Cancel();
        if (_watcher is not null) { _watcher.Created -= OnCreated; _watcher.Dispose(); }
    }
}
