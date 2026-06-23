using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClaudeCodeSpeaketh.ViewModels;

// One Claude session row in the Sessions tab; toggling Enabled mutes/unmutes it.
internal sealed partial class SessionToggle : ObservableObject
{
    private readonly Action<SessionToggle> _onChanged;

    public string Id { get; }
    public string Display { get; }
    public string Detail { get; }   // branch · full path · last-active

    [ObservableProperty] private bool _enabled;

    public SessionToggle(string id, string display, string detail, bool enabled, Action<SessionToggle> onChanged)
    {
        Id = id;
        Display = display;
        Detail = detail;
        _enabled = enabled;
        _onChanged = onChanged;
    }

    partial void OnEnabledChanged(bool value) => _onChanged(this);
}
