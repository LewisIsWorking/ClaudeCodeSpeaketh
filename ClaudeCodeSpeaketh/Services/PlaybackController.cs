using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClaudeCodeSpeaketh.Services;

// A playback that can be paused/resumed in-process (the karaoke players).
// The detached-PowerShell SpeechRunner does NOT implement this -- it can only be
// skipped (killed), which is why the controller exposes CanPause separately.
internal interface IControllablePlayback
{
    void Pause();
    void Resume();
}

// Shared transport state + commands, bound by BOTH the main window's control bar
// and the karaoke overlay. The SpeechQueueProcessor owns the actual playback and
// wires the hooks below for each utterance; this type just reflects state to the
// UI (marshalling onto the UI thread) and forwards button presses to those hooks.
internal sealed partial class PlaybackController : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PauseResumeLabel))]
    private bool _isSpeaking;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PauseResumeLabel))]
    private bool _isPaused;

    // True only while an in-process (karaoke) player is active. The plain fallback
    // can't be paused, so the Pause button greys out for it.
    [ObservableProperty] private bool _canPause;

    // True when there is a previous response to replay.
    [ObservableProperty] private bool _canGoBack;

    public string PauseResumeLabel => IsPaused ? "▶ Resume" : "⏸ Pause";

    // Wired by the processor before each utterance; cleared when it ends.
    public Action? PauseHook { get; set; }
    public Action? ResumeHook { get; set; }
    public Action? SkipHook { get; set; }
    public Action? BackHook { get; set; }

    [RelayCommand]
    private void PauseResume()
    {
        if (!IsSpeaking || !CanPause) return;
        if (IsPaused) { ResumeHook?.Invoke(); IsPaused = false; }
        else { PauseHook?.Invoke(); IsPaused = true; }
    }

    [RelayCommand]
    private void Skip()
    {
        if (IsSpeaking) SkipHook?.Invoke();
    }

    [RelayCommand]
    private void Back()
    {
        if (CanGoBack) BackHook?.Invoke();
    }

    // --- processor -> UI state notifications (thread-safe) --------------------

    public void OnStarted(bool canPause) =>
        OnUi(() => { IsSpeaking = true; IsPaused = false; CanPause = canPause; });

    public void OnStopped() =>
        OnUi(() => { IsSpeaking = false; IsPaused = false; CanPause = false; });

    public void SetCanPause(bool value) => OnUi(() => CanPause = value);

    public void SetCanGoBack(bool value) => OnUi(() => CanGoBack = value);

    private static void OnUi(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess()) action();
        else Dispatcher.UIThread.Post(action);
    }
}
