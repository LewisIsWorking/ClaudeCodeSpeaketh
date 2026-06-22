using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeSpeaketh.Models;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// "Neural (edge-tts)" tab: install edge-tts, pick a neural voice (default Emily,
// the free Irish female), and preview. Free + keyless but needs internet/Python.
internal partial class NeuralViewModel : ObservableObject
{
    private const string DefaultVoice = "en-IE-EmilyNeural";
    private readonly EdgeTtsService _edge;
    private string _configuredVoice = DefaultVoice;

    public ObservableCollection<EdgeVoiceInfo> Voices { get; } = new();

    [ObservableProperty] private EdgeVoiceInfo? _selectedVoice;
    [ObservableProperty] private string _status = "Checking edge-tts...";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isAvailable;

    public NeuralViewModel(EdgeTtsService edge) => _edge = edge;

    public void LoadFrom(TtsConfig cfg) { _configuredVoice = cfg.Edge.Voice; _ = InitAsync(); }
    public void ApplyTo(TtsConfig cfg) { if (SelectedVoice is not null) cfg.Edge.Voice = SelectedVoice.ShortName; }

    private async Task InitAsync()
    {
        IsBusy = true;
        IsAvailable = await Task.Run(() => _edge.IsAvailable());
        if (IsAvailable) await LoadVoicesAsync();
        else { Status = "edge-tts isn't installed. Click 'Install neural voices' (needs Python + internet)."; IsBusy = false; }
    }

    [RelayCommand]
    private async Task Install()
    {
        IsBusy = true; Status = "Installing edge-tts via pip (one-time, ~10s)...";
        IsAvailable = await Task.Run(() => _edge.Install());
        if (IsAvailable) { await LoadVoicesAsync(); }
        else { Status = "Install failed -- is Python on PATH? Try: pip install edge-tts"; IsBusy = false; }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        if (!IsAvailable) await InitAsync();
        else await LoadVoicesAsync();
    }

    private async Task LoadVoicesAsync()
    {
        IsBusy = true; Status = "Loading neural voices...";
        var voices = await Task.Run(() => _edge.GetEnglishVoices());
        Voices.Clear();
        foreach (var v in voices) Voices.Add(v);
        SelectedVoice = Voices.FirstOrDefault(v => v.ShortName == _configuredVoice)
                        ?? Voices.FirstOrDefault(v => v.ShortName == DefaultVoice)
                        ?? Voices.FirstOrDefault();
        Status = Voices.Count > 0
            ? $"{Voices.Count} English neural voices. Default: Emily (Irish, female)."
            : "Couldn't load voices (no internet?).";
        IsBusy = false;
    }

    [RelayCommand]
    private void Preview()
    {
        if (SelectedVoice is not null)
            _edge.Preview(SelectedVoice.ShortName, "Hello! This is how Claude Code will sound when it reads its responses.");
    }
}
