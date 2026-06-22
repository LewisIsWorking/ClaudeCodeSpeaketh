using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace ClaudeCodeSpeaketh.Services;

// Wraps Velopack's UpdateManager. Today it reads from a LOCAL folder feed (the
// repo's Releases dir) so updates work + are testable on this machine with no
// hosting. Swap DefaultFeed to a GithubSource / URL for real distribution.
[SupportedOSPlatform("windows")]
internal sealed class UpdateService
{
    // Local feed for now. To ship: replace with e.g.
    //   new UpdateManager(new GithubSource("https://github.com/LewisIsWorking/ClaudeCodeSpeaketh", null, false))
    public const string DefaultFeed =
        @"C:\Users\Lewis\RiderProjects\ClaudeCodeSpeaketh\Releases";

    private readonly UpdateManager _mgr;

    public UpdateService(string? feed = null)
        => _mgr = new UpdateManager(feed ?? DefaultFeed);

    /// <summary>True only when running as a Velopack-installed app (not a dev build).</summary>
    public bool IsInstalled => _mgr.IsInstalled;

    public string CurrentVersion => _mgr.CurrentVersion?.ToString() ?? "dev";

    public Task<UpdateInfo?> CheckAsync() => _mgr.CheckForUpdatesAsync();

    public Task DownloadAsync(UpdateInfo info, Action<int> onProgress)
        => _mgr.DownloadUpdatesAsync(info, onProgress);

    /// <summary>Applies the update and relaunches the new version (exits this process).</summary>
    public void ApplyAndRestart(UpdateInfo info) => _mgr.ApplyUpdatesAndRestart(info);
}
