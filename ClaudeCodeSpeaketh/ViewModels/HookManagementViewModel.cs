using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// Hook tab: install / remove the Claude Code Stop hook in settings.json and
// (re)deploy the hook scripts. Preserves every other hook/setting.
internal partial class HookManagementViewModel : ObservableObject
{
    private readonly HookInstallService _install;
    private readonly HookScriptDeployService _deploy;

    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private string _status = "";

    public HookManagementViewModel(HookInstallService install, HookScriptDeployService deploy)
    {
        _install = install;
        _deploy = deploy;
        Refresh();
    }

    private void Refresh()
    {
        IsInstalled = _install.IsInstalled();
        Status = IsInstalled
            ? "The Stop hook is installed. Claude Code will speak its responses."
            : "The Stop hook is not installed. Click Install to enable spoken responses.";
    }

    [RelayCommand]
    private void Install()
    {
        try
        {
            _deploy.DeployAll();
            _install.Install();
            Refresh();
            Status = "Installed. Restart Claude Code (or approve the hook via /hooks) to apply.";
        }
        catch (Exception ex) { Status = "Install failed: " + ex.Message; }
    }

    [RelayCommand]
    private void Uninstall()
    {
        try { _install.Uninstall(); Refresh(); Status = "Removed the Stop hook. Other hooks were left untouched."; }
        catch (Exception ex) { Status = "Uninstall failed: " + ex.Message; }
    }

    [RelayCommand]
    private void RedeployScripts()
    {
        try { var n = _deploy.DeployAll().Length; Status = $"Re-deployed {n} hook script(s) to ~/.claude/hooks."; }
        catch (Exception ex) { Status = "Re-deploy failed: " + ex.Message; }
    }
}
