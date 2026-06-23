using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ClaudeCodeSpeaketh.Services;

// Discovers Claude Code sessions from the transcript store (~/.claude/projects/
// <encoded-project>/<session-id>.jsonl) so the Sessions tab lists already-running
// terminals without waiting for them to speak.
internal sealed class SessionDiscoveryService
{
    private readonly string _projectsDir;

    public SessionDiscoveryService(string hooksDir)
    {
        var claudeDir = Directory.GetParent(hooksDir)!.FullName;
        _projectsDir = Path.Combine(claudeDir, "projects");
    }

    /// <summary>Recent sessions (mtime within maxAgeHours), newest first, capped.</summary>
    public IReadOnlyList<(string Id, string Cwd)> Discover(int maxAgeHours = 24, int cap = 25)
    {
        if (!Directory.Exists(_projectsDir)) return Array.Empty<(string, string)>();
        var cutoff = DateTime.UtcNow.AddHours(-maxAgeHours);

        IEnumerable<string> files;
        try { files = Directory.EnumerateFiles(_projectsDir, "*.jsonl", SearchOption.AllDirectories); }
        catch { return Array.Empty<(string, string)>(); }

        return files
            .Select(f => new FileInfo(f))
            .Where(fi => fi.LastWriteTimeUtc >= cutoff)
            // Skip subagent sidechain transcripts (agent-*) -- not real terminals.
            .Where(fi => !fi.Name.StartsWith("agent-", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(fi => fi.LastWriteTimeUtc)
            .Take(cap)
            .Select(fi => (Id: Path.GetFileNameWithoutExtension(fi.Name), Cwd: ReadCwd(fi.FullName)))
            .Where(s => s.Id.Length > 0)
            .ToList();
    }

    // cwd isn't on the first (summary) line, so scan a few lines for it.
    private static string ReadCwd(string path)
    {
        try
        {
            using var reader = new StreamReader(path);
            for (var i = 0; i < 60; i++)
            {
                var line = reader.ReadLine();
                if (line is null) break;
                if (line.Length == 0 || !line.Contains("\"cwd\"")) continue;
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("cwd", out var cwd) && cwd.ValueKind == JsonValueKind.String)
                    return cwd.GetString() ?? "";
            }
        }
        catch { }
        return "";
    }
}
