using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClaudeCodeSpeaketh.ViewModels;

// One Claude session row in the Sessions tab; toggling Enabled mutes/unmutes it.
internal sealed partial class SessionToggle : ObservableObject
{
    private readonly Action<SessionToggle> _onChanged;

    public string Id { get; }
    public string Display { get; }

    [ObservableProperty] private bool _enabled;

    public SessionToggle(string id, string display, bool enabled, Action<SessionToggle> onChanged)
    {
        Id = id;
        Display = display;
        _enabled = enabled;
        _onChanged = onChanged;
    }

    partial void OnEnabledChanged(bool value) => _onChanged(this);
}
