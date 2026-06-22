using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace ClaudeCodeSpeaketh.Services;

// Wraps Velopack's UpdateManager, reading releases from the public GitHub repo
// (tokenless GithubSource). `vpk upload github` publishes each version there.
[SupportedOSPlatform("windows")]
internal sealed class UpdateService
{
    public const string RepoUrl = "https://github.com/LewisIsWorking/ClaudeCodeSpeaketh";

    private readonly UpdateManager _mgr;

    // accessToken null = public repo; prerelease false.
    public UpdateService()
        => _mgr = new UpdateManager(new GithubSource(RepoUrl, null, false));

    /// <summary>True only when running as a Velopack-installed app (not a dev build).</summary>
    public bool IsInstalled => _mgr.IsInstalled;

    public string CurrentVersion => _mgr.CurrentVersion?.ToString() ?? "dev";

    public Task<UpdateInfo?> CheckAsync() => _mgr.CheckForUpdatesAsync();

    public Task DownloadAsync(UpdateInfo info, Action<int> onProgress)
        => _mgr.DownloadUpdatesAsync(info, onProgress);

    /// <summary>Applies the update and relaunches the new version (exits this process).</summary>
    public void ApplyAndRestart(UpdateInfo info) => _mgr.ApplyUpdatesAndRestart(info);
}
