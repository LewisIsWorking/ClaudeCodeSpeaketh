using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Velopack;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// Updates tab: shows the installed version, checks the feed, and downloads/applies
// with a REAL percentage (Velopack reports 0-100 during download).
[SupportedOSPlatform("windows")]
internal partial class UpdateViewModel : ObservableObject
{
    private readonly UpdateService _updates;
    private UpdateInfo? _pending;

    public string VersionLabel { get; }

    [ObservableProperty] private string _status;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _updateReady;     // a download finished, ready to apply
    [ObservableProperty] private double _downloadPercent;
    [ObservableProperty] private bool _showProgress;

    public UpdateViewModel(UpdateService updates)
    {
        _updates = updates;
        VersionLabel = "v" + _updates.CurrentVersion;
        Status = _updates.IsInstalled
            ? "Click Check for updates."
            : "Running a dev build (updates apply only to the installed version).";
    }

    [RelayCommand]
    private async Task CheckAsync()
    {
        if (!_updates.IsInstalled) { Status = "Not an installed build -- nothing to update."; return; }
        IsBusy = true; ShowProgress = false; UpdateReady = false;
        Status = "Checking for updates...";
        try
        {
            _pending = await _updates.CheckAsync();
            Status = _pending is null
                ? "You're on the latest version."
                : $"Update available: {_pending.TargetFullRelease.Version}. Click Download & install.";
        }
        catch (Exception ex) { Status = "Update check failed: " + ex.Message; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DownloadAndApplyAsync()
    {
        if (_pending is null) return;
        IsBusy = true; ShowProgress = true; DownloadPercent = 0;
        Status = "Downloading update...";
        try
        {
            await _updates.DownloadAsync(_pending, p =>
                Dispatcher.UIThread.Post(() => DownloadPercent = p));
            UpdateReady = true;
            Status = "Download complete -- restarting into the new version...";
            _updates.ApplyAndRestart(_pending);   // exits + relaunches
        }
        catch (Exception ex) { Status = "Update failed: " + ex.Message; IsBusy = false; ShowProgress = false; }
    }
}
