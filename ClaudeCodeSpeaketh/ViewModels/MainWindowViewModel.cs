using System;
using System.Runtime.Versioning;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClaudeCodeSpeaketh.Models;
using ClaudeCodeSpeaketh.Services;

namespace ClaudeCodeSpeaketh.ViewModels;

// Composition root for the window: owns services + the per-tab sub-ViewModels,
// loads/saves the shared TtsConfig, runs the resident speech daemon, and
// (re)deploys the hook scripts on save.
[SupportedOSPlatform("windows")]
internal partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly ConfigService _config;
    private readonly HookScriptDeployService _deploy;
    private readonly DaemonHeartbeat? _heartbeat;
    private readonly SpeechQueueProcessor? _processor;
    private TtsConfig _model;
    private bool _ready;

    public GeneralViewModel General { get; }
    public NeuralViewModel Neural { get; }
    public SapiVoiceViewModel Sapi { get; }
    public VoiceManagementViewModel Voices { get; }
    public SessionsViewModel Sessions { get; }
    public KaraokeViewModel Karaoke { get; }
    public HookManagementViewModel Hook { get; }
    public UpdateViewModel Update { get; }

    [ObservableProperty] private string _status = "Ready.";

    public MainWindowViewModel()
    {
        _config = new ConfigService();
        _deploy = new HookScriptDeployService(_config.HooksDir);
        _model = _config.Load();

        General = new GeneralViewModel();
        var preview = new VoicePreviewService();
        Sapi = new SapiVoiceViewModel(new SapiVoiceService(), preview, General);
        Neural = new NeuralViewModel(new EdgeTtsService(_config.HooksDir));

        // After installing the Irish pack, re-enumerate so Orla shows in the picker.
        Voices = new VoiceManagementViewModel(
            new IrishVoiceInstallService(), () => Sapi.LoadFrom(_model));

        Update = new UpdateViewModel(new UpdateService());
        Sessions = new SessionsViewModel(() => _model, cfg => _config.Save(cfg),
            new SessionDiscoveryService(_config.HooksDir));
        Karaoke = new KaraokeViewModel();
        Hook = new HookManagementViewModel(new HookInstallService(_config.HooksDir), _deploy);

        General.LoadFrom(_model);
        Sapi.LoadFrom(_model);
        Neural.LoadFrom(_model);
        Karaoke.LoadFrom(_model);

        // Persist every setting change immediately so the daemon (which reads config
        // fresh each turn) reflects it on the next response -- no Save click needed.
        General.PropertyChanged += (_, _) => PersistLive();
        Sapi.PropertyChanged += (_, _) => PersistLive();
        Neural.PropertyChanged += (_, _) => PersistLive();
        Karaoke.PropertyChanged += (_, _) => PersistLive();
        _ready = true;

        // Resident daemon: heartbeat so the hook enqueues to us, plus the queue
        // processor that serializes playback across sessions. Reads fresh config
        // each item so engine/voice/session toggles apply live. Single-instance:
        // if another copy already owns the queue, this one is just a config editor.
        _deploy.DeployAll();   // ensure hooks are current for queue mode
        if (!DaemonHeartbeat.IsAlive(_config.HooksDir))
        {
            _heartbeat = new DaemonHeartbeat(_config.HooksDir);
            _processor = new SpeechQueueProcessor(_config.HooksDir, () => _config.Load(),
                item => Dispatcher.UIThread.Post(() => Sessions.NoteSession(item.SessionId, item.Cwd)));
            _processor.Start();
        }
    }

    public void Dispose()
    {
        _processor?.Dispose();
        _heartbeat?.Dispose();
    }

    // Writes the current settings to disk on any change (live).
    private void PersistLive()
    {
        if (!_ready) return;
        try
        {
            General.ApplyTo(_model);
            Sapi.ApplyTo(_model);
            Neural.ApplyTo(_model);
            Karaoke.ApplyTo(_model);
            _config.Save(_model);
        }
        catch { /* best-effort; explicit Save surfaces errors */ }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            General.ApplyTo(_model);
            Sapi.ApplyTo(_model);
            Neural.ApplyTo(_model);
            Karaoke.ApplyTo(_model);
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
