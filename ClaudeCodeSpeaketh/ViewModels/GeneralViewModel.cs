using CommunityToolkit.Mvvm.ComponentModel;
using ClaudeCodeSpeaketh.Models;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// General tab: master on/off, read-length (ALL by default), and the SAPI
// rate/volume sliders. Reads from / writes back to the shared TtsConfig.
internal partial class GeneralViewModel : ObservableObject
{
    private readonly IStartupService _startup;

    [ObservableProperty] private bool _enabled = true;

    // Launch at Windows sign-in. Backed by the registry (via IStartupService),
    // NOT tts-config.json -- so it is loaded once in the ctor and written back
    // whenever the box is toggled.
    [ObservableProperty] private bool _startAtStartup;

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

    public GeneralViewModel(IStartupService startup)
    {
        _startup = startup;
        // Set the backing field directly: reflect the current registry state in
        // the checkbox WITHOUT re-triggering a registry write on load.
        _startAtStartup = startup.IsEnabled();
    }

    // Toggling the box adds/removes the Run-key registration immediately.
    partial void OnStartAtStartupChanged(bool value) => _startup.SetEnabled(value);

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
