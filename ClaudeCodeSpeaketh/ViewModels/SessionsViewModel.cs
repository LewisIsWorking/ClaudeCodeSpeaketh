using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ClaudeCodeSpeaketh.Models;

namespace ClaudeCodeSpeaketh.ViewModels;

// Sessions tab: lists Claude sessions the daemon has heard from, each with an
// on/off toggle. Toggling persists to config immediately so it takes effect.
internal partial class SessionsViewModel : ObservableObject
{
    private readonly Func<TtsConfig> _load;
    private readonly Action<TtsConfig> _save;

    public ObservableCollection<SessionToggle> Sessions { get; } = new();

    [ObservableProperty] private string _hint =
        "Sessions appear here as each Claude terminal speaks. Untick one to mute just that terminal.";

    public SessionsViewModel(Func<TtsConfig> load, Action<TtsConfig> save)
    {
        _load = load;
        _save = save;
    }

    // Called (on the UI thread) when the daemon sees a session id.
    public void NoteSession(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (Sessions.Any(s => s.Id == id)) return;
        var cfg = _load();
        var enabled = !cfg.SessionOverrides.TryGetValue(id, out var on) || on;
        Sessions.Add(new SessionToggle(id, enabled, Persist));
    }

    private void Persist(SessionToggle t)
    {
        var cfg = _load();
        cfg.SessionOverrides[t.Id] = t.Enabled;
        _save(cfg);
    }
}
