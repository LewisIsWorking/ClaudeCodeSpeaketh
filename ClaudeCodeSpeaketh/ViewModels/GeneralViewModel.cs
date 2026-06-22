using CommunityToolkit.Mvvm.ComponentModel;
using ClaudeCodeSpeaketh.Models;

namespace ClaudeCodeSpeaketh.ViewModels;

// General tab: master on/off, read-length (ALL by default), and the SAPI
// rate/volume sliders. Reads from / writes back to the shared TtsConfig.
internal partial class GeneralViewModel : ObservableObject
{
    [ObservableProperty] private bool _enabled = true;

    // Engine: true = Neural (edge-tts), false = Classic (SAPI). Neural is default.
    [ObservableProperty] private bool _useNeural = true;

    // "Speak entire response" == maxChars <= 0. When unchecked, MaxCharsValue applies.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxCharsEnabled))]
    private bool _speakEntireResponse = true;

    [ObservableProperty] private int _maxCharsValue = 1500;
    [ObservableProperty] private int _rate;        // -10..10
    [ObservableProperty] private int _volume = 100; // 0..100

    public bool MaxCharsEnabled => !SpeakEntireResponse;

    public void LoadFrom(TtsConfig cfg)
    {
        Enabled = cfg.Enabled;
        UseNeural = cfg.Engine != "sapi";
        SpeakEntireResponse = cfg.MaxChars <= 0;
        MaxCharsValue = cfg.MaxChars > 0 ? cfg.MaxChars : 1500;
        Rate = cfg.Sapi.Rate;
        Volume = cfg.Sapi.Volume;
    }

    public void ApplyTo(TtsConfig cfg)
    {
        cfg.Enabled = Enabled;
        cfg.Engine = UseNeural ? "edge" : "sapi";
        cfg.MaxChars = SpeakEntireResponse ? 0 : MaxCharsValue;
        cfg.Sapi.Rate = Rate;
        cfg.Sapi.Volume = Volume;
    }
}
