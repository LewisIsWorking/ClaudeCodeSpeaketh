using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeSpeaketh.Models;

namespace ClaudeCodeSpeaketh.ViewModels;

// Karaoke tab: toggle the companion word-highlight window and pick the highlight
// colour. Only active with the resident app + the edge (neural) engine.
internal partial class KaraokeViewModel : ObservableObject
{
    [ObservableProperty] private bool _enabled = true;
    [ObservableProperty] private string _colorHex = "#FFD54A";

    public void LoadFrom(TtsConfig cfg)
    {
        Enabled = cfg.Karaoke.Enabled;
        ColorHex = cfg.Karaoke.ColorHex;
    }

    public void ApplyTo(TtsConfig cfg)
    {
        cfg.Karaoke.Enabled = Enabled;
        cfg.Karaoke.ColorHex = ColorHex;
    }

    [RelayCommand]
    private void SetColor(string hex) => ColorHex = hex;
}
