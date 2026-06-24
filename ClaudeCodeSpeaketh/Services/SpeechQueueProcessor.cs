using System;
using System.Collections.Generic;
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
    private readonly Action<QueueItem>? _onSessionSeen;
    private readonly PlaybackController _controller;
    private readonly SpeechRunner _runner;
    private readonly EdgeKaraokePlayer _karaoke;
    private readonly SapiKaraokePlayer _sapiKaraoke;
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _stop = new();

    // Recently-spoken utterances (newest last), for the Back button. Capped.
    private readonly List<QueueItem> _history = new();
    // Queue files we wrote ourselves for a replay -- so Drain doesn't push them
    // back onto the history (which would make Back ping-pong forever).
    private readonly HashSet<string> _replayPaths = new();

    private FileSystemWatcher? _watcher;
    private volatile string? _currentSession;
    private CancellationTokenSource? _currentCts;

    public SpeechQueueProcessor(string hooksDir, Func<TtsConfig> config,
        PlaybackController controller, Action<QueueItem>? onSessionSeen = null)
    {
        _queueDir = Path.Combine(hooksDir, "tts-queue");
        Directory.CreateDirectory(_queueDir);
        _config = config;
        _controller = controller;
        _onSessionSeen = onSessionSeen;
        _runner = new SpeechRunner(hooksDir);
        _karaoke = new EdgeKaraokePlayer(hooksDir, controller);
        _sapiKaraoke = new SapiKaraokePlayer(controller);
        _controller.BackHook = Back;
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
            _onSessionSeen?.Invoke(item);
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
            var isReplay = _replayPaths.Remove(next);
            TryDelete(next);
            if (item is null) continue;

            var cfg = _config();
            if (!cfg.Enabled) continue;                       // master off -> drop
            if (cfg.SessionOverrides.TryGetValue(item.SessionId, out var on) && !on) continue; // muted session

            if (!isReplay) RecordHistory(item);

            _currentSession = item.SessionId;
            _currentCts = new CancellationTokenSource();
            var cts = _currentCts;

            // Only the in-process karaoke players can be paused; the plain
            // fallback can only be skipped (killed). Wire the transport hooks.
            IControllablePlayback? controllable = cfg.Karaoke.Enabled
                ? (cfg.Engine == "edge" ? _karaoke : _sapiKaraoke)
                : null;
            _controller.PauseHook = () => controllable?.Pause();
            _controller.ResumeHook = () => controllable?.Resume();
            _controller.SkipHook = () => { try { cts.Cancel(); } catch { } };
            _controller.OnStarted(canPause: controllable is not null);
            try
            {
                // Karaoke window (edge or SAPI) when enabled; otherwise -- or if
                // karaoke synthesis fails -- fall back to plain speak.
                var handled = false;
                if (controllable is not null)
                {
                    handled = cfg.Engine == "edge"
                        ? _karaoke.Play(item.Text, cfg, cts.Token)
                        : _sapiKaraoke.Play(item.Text, cfg, cts.Token);
                }
                if (!handled)
                {
                    _controller.SetCanPause(false);   // plain path can't pause
                    _runner.Speak(item.Text, cts.Token);
                }
            }
            catch { }
            finally
            {
                _controller.OnStopped();
                _currentSession = null; _currentCts.Dispose(); _currentCts = null;
            }
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

    // Append a freshly-spoken utterance to the capped history and let the UI know
    // there is something to go Back to.
    private void RecordHistory(QueueItem item)
    {
        lock (_history)
        {
            _history.Add(item);
            if (_history.Count > 20) _history.RemoveAt(0);
        }
        _controller.SetCanGoBack(true);
    }

    // Back: replay the previous response. If something is playing, the previous is
    // the one before it; otherwise it's the last thing spoken. Re-enqueues a copy
    // (marked as a replay so it doesn't re-enter the history) and interrupts the
    // current utterance so the replay starts immediately.
    private void Back()
    {
        QueueItem target;
        lock (_history)
        {
            if (_history.Count == 0) return;
            var speaking = _currentSession is not null;
            target = speaking && _history.Count >= 2 ? _history[^2] : _history[^1];
        }

        try
        {
            var name = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-" +
                       Guid.NewGuid().ToString("N")[..6] + ".json";
            var path = Path.Combine(_queueDir, name);
            _replayPaths.Add(path);
            File.WriteAllText(path, JsonSerializer.Serialize(target, Json));
        }
        catch { return; }

        _currentCts?.Cancel();   // stop whatever is playing so the replay plays now
        _signal.Release();
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
