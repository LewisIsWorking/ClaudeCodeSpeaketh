using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeSpeaketh.Models;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// Sessions tab: lists recent Claude sessions (discovered from the transcript
// store, plus any the daemon hears live), each with an on/off toggle that
// persists immediately so it takes effect.
internal partial class SessionsViewModel : ObservableObject
{
    private readonly Func<TtsConfig> _load;
    private readonly Action<TtsConfig> _save;
    private readonly SessionDiscoveryService _discovery;

    public ObservableCollection<SessionToggle> Sessions { get; } = new();

    [ObservableProperty] private string _hint =
        "Recent Claude terminals (last 24h). Untick one to mute just that terminal. New ones appear as they speak.";

    public SessionsViewModel(Func<TtsConfig> load, Action<TtsConfig> save, SessionDiscoveryService discovery)
    {
        _load = load;
        _save = save;
        _discovery = discovery;
        Refresh();
    }

    [RelayCommand]
    private void Refresh()
    {
        foreach (var (id, cwd) in _discovery.Discover()) NoteSession(id, cwd);
    }

    // Called (on the UI thread) when the daemon sees a session.
    public void NoteSession(string id, string cwd)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (Sessions.Any(s => s.Id == id)) return;
        var cfg = _load();
        var enabled = !cfg.SessionOverrides.TryGetValue(id, out var on) || on;
        Sessions.Add(new SessionToggle(id, BuildLabel(id, cwd), enabled, Persist));
    }

    // Prefer the terminal's project-folder name; fall back to a short id.
    private static string BuildLabel(string id, string cwd)
    {
        var shortId = id.Length > 8 ? id.Substring(0, 8) : id;
        var leaf = string.IsNullOrWhiteSpace(cwd)
            ? "" : Path.GetFileName(cwd.TrimEnd('\\', '/'));
        return leaf.Length > 0 ? $"{leaf}   ({shortId})" : shortId;
    }

    private void Persist(SessionToggle t)
    {
        var cfg = _load();
        cfg.SessionOverrides[t.Id] = t.Enabled;
        _save(cfg);
    }
}
