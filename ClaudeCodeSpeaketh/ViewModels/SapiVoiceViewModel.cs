using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeSpeaketh.Models;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// SAPI Voices tab: pick the classic voice the hook speaks with, and audition it.
// (OneCore/Irish install + mirror is added in Phase 2.)
[SupportedOSPlatform("windows")]
internal partial class SapiVoiceViewModel : ObservableObject
{
    private readonly SapiVoiceService _voices;
    private readonly VoicePreviewService _preview;
    private readonly GeneralViewModel _general;

    public ObservableCollection<SapiVoiceInfo> Voices { get; } = new();

    [ObservableProperty] private SapiVoiceInfo? _selectedVoice;

    // Suppresses auto-preview while LoadFrom sets the initial selection.
    private bool _loading;

    public SapiVoiceViewModel(SapiVoiceService voices, VoicePreviewService preview, GeneralViewModel general)
    {
        _voices = voices;
        _preview = preview;
        _general = general;
    }

    public void LoadFrom(TtsConfig cfg)
    {
        _loading = true;
        Voices.Clear();
        foreach (var v in _voices.GetInstalledVoices()) Voices.Add(v);
        SelectedVoice = Voices.FirstOrDefault(v => v.Name == cfg.Sapi.Voice)
                        ?? Voices.FirstOrDefault();
        _loading = false;
    }

    // Auto-audition the voice the moment the user picks a different one.
    partial void OnSelectedVoiceChanged(SapiVoiceInfo? value)
    {
        if (_loading || value is null) return;
        _preview.PreviewSapi(value.Name, _general.Rate, _general.Volume);
    }

    public void ApplyTo(TtsConfig cfg)
    {
        if (SelectedVoice is not null) cfg.Sapi.Voice = SelectedVoice.Name;
    }

    [RelayCommand]
    private void Preview()
    {
        if (SelectedVoice is null) return;
        _preview.PreviewSapi(SelectedVoice.Name, _general.Rate, _general.Volume);
    }

    [RelayCommand]
    private void StopPreview() => _preview.Stop();
}
