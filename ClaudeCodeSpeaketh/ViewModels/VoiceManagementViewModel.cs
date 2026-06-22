using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeSpeaketh.Helpers;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// "Get Voices" tab: install the Irish voice (Orla). Modern System.Speech reads
// OneCore voices directly, so once the en-IE pack is installed the voice simply
// appears in the SAPI picker after a refresh -- no registry mirror needed.
[SupportedOSPlatform("windows")]
internal partial class VoiceManagementViewModel : ObservableObject
{
    private readonly IrishVoiceInstallService _install;
    private readonly Action _refreshVoices;

    [ObservableProperty] private string _status =
        "Click below to install the Irish voice (Orla), then it appears in the SAPI Voices tab.";
    [ObservableProperty] private bool _isBusy;

    public VoiceManagementViewModel(IrishVoiceInstallService install, Action refreshVoices)
    {
        _install = install;
        _refreshVoices = refreshVoices;
    }

    [RelayCommand]
    private async Task InstallIrishAsync()
    {
        IsBusy = true;
        Status = "Installing the Irish voice (Orla) -- approve the UAC prompt; this can take a few minutes...";
        var result = await Task.Run(() => _install.Install());
        IsBusy = false;

        if (result.Declined) { Status = "Cancelled at the UAC prompt."; return; }
        if (!result.Started) { Status = "Failed to elevate: " + (result.Error ?? "unknown error"); return; }
        if (result.ExitCode != 0) { Status = $"Installer exited with code {result.ExitCode}."; return; }

        _refreshVoices();
        Status = "Install complete. Look for 'Microsoft Orla' in the SAPI Voices tab. " +
                 "If it's missing, sign out and back in, then click Refresh voices.";
    }

    [RelayCommand]
    private void RefreshVoices()
    {
        _refreshVoices();
        Status = "Voice list refreshed -- check the SAPI Voices tab.";
    }
}
