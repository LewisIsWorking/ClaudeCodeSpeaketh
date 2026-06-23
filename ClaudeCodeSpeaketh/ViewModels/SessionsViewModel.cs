using System;
using System.Collections.Generic;
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
        foreach (var d in _discovery.Discover())
            NoteSession(d.Id, d.Cwd, d.Branch, d.LastActiveUtc);
    }

    // Called (on the UI thread) for discovered + live-heard sessions.
    public void NoteSession(string id, string cwd, string branch = "", DateTime? lastActiveUtc = null)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (Sessions.Any(s => s.Id == id)) return;
        var cfg = _load();
        var enabled = !cfg.SessionOverrides.TryGetValue(id, out var on) || on;
        Sessions.Add(new SessionToggle(id, BuildLabel(id, cwd), BuildDetail(cwd, branch, lastActiveUtc), enabled, Persist));
    }

    // Prefer the terminal's project-folder name; fall back to a short id.
    private static string BuildLabel(string id, string cwd)
    {
        var shortId = id.Length > 8 ? id.Substring(0, 8) : id;
        var leaf = string.IsNullOrWhiteSpace(cwd)
            ? "" : Path.GetFileName(cwd.TrimEnd('\\', '/'));
        return leaf.Length > 0 ? $"{leaf}   ({shortId})" : shortId;
    }

    // Second line: branch · full path · last-active.
    private static string BuildDetail(string cwd, string branch, DateTime? lastActiveUtc)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(branch)) parts.Add("⎇ " + branch);
        if (!string.IsNullOrWhiteSpace(cwd)) parts.Add(cwd);
        parts.Add(lastActiveUtc is { } t ? RelativeTime(t) : "active now");
        return string.Join("   ·   ", parts);
    }

    private static string RelativeTime(DateTime utc)
    {
        var span = DateTime.UtcNow - utc;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        return $"{(int)span.TotalDays}d ago";
    }

    private void Persist(SessionToggle t)
    {
        var cfg = _load();
        cfg.SessionOverrides[t.Id] = t.Enabled;
        _save(cfg);
    }
}
