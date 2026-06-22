using System;
using System.Runtime.Versioning;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeSpeaketh.Models;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// Composition root for the window: owns services + the per-tab sub-ViewModels,
// loads/saves the shared TtsConfig, and (re)deploys the hook scripts on save.
[SupportedOSPlatform("windows")]
internal partial class MainWindowViewModel : ObservableObject
{
    private readonly ConfigService _config;
    private readonly HookScriptDeployService _deploy;
    private TtsConfig _model;

    public GeneralViewModel General { get; }
    public SapiVoiceViewModel Sapi { get; }

    [ObservableProperty] private string _status = "Ready.";

    public MainWindowViewModel()
    {
        _config = new ConfigService();
        _deploy = new HookScriptDeployService(_config.HooksDir);
        _model = _config.Load();

        General = new GeneralViewModel();
        var preview = new VoicePreviewService();
        Sapi = new SapiVoiceViewModel(new SapiVoiceService(), preview, General);

        General.LoadFrom(_model);
        Sapi.LoadFrom(_model);
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            General.ApplyTo(_model);
            Sapi.ApplyTo(_model);
            _config.Save(_model);

            var written = _deploy.DeployAll();
            Status = $"Saved {_config.ConfigPath}. Deployed {written.Length} hook script(s). " +
                     "Takes effect on Claude Code's next response.";
        }
        catch (Exception ex)
        {
            Status = "Save failed: " + ex.Message;
        }
    }
}
