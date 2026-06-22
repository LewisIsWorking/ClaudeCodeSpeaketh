using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// "Get Voices" tab: install the Irish voice (Orla) with a REAL percentage parsed
// from DISM's output. Once installed, modern System.Speech sees it directly, so a
// refresh surfaces it in the SAPI picker -- no registry mirror needed.
[SupportedOSPlatform("windows")]
internal partial class VoiceManagementViewModel : ObservableObject
{
    private readonly IrishVoiceInstallService _install;
    private readonly Action _refreshVoices;

    [ObservableProperty] private string _status =
        "Click below to install the Irish voice (Orla), then it appears in the SAPI Voices tab.";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private double _installPercent;
    [ObservableProperty] private bool _installIndeterminate;

    public VoiceManagementViewModel(IrishVoiceInstallService install, Action refreshVoices)
    {
        _install = install;
        _refreshVoices = refreshVoices;
    }

    [RelayCommand]
    private async Task InstallIrishAsync()
    {
        IsBusy = true;
        InstallIndeterminate = true;   // until DISM emits the first %
        InstallPercent = 0;
        Status = "Installing the Irish voice (Orla) -- approve the UAC prompt; downloading from Windows Update...";

        var proc = _install.Start();
        if (proc is null)
        {
            IsBusy = false; InstallIndeterminate = false;
            Status = "Cancelled at the UAC prompt (or failed to elevate).";
            return;
        }

        await Task.Run(async () =>
        {
            while (!proc.HasExited)
            {
                var p = _install.ReadPercent();
                if (p >= 0) Dispatcher.UIThread.Post(() => { InstallIndeterminate = false; InstallPercent = p; });
                await Task.Delay(400);
            }
        });

        int exit; try { exit = proc.ExitCode; } catch { exit = 0; }
        InstallIndeterminate = false; InstallPercent = 100; IsBusy = false;

        if (exit != 0)
        {
            Status = $"Installer exited with code {exit}. Log: {_install.ProgressFile}";
            return;
        }
        _refreshVoices();
        Status = "Install complete. Pick 'Microsoft Orla' in the SAPI Voices tab. " +
                 "If it's missing, sign out/in then click Refresh voices.";
    }

    [RelayCommand]
    private void RefreshVoices()
    {
        _refreshVoices();
        Status = "Voice list refreshed -- check the SAPI Voices tab.";
    }
}
