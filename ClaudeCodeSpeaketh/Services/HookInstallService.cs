using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClaudeCodeSpeaketh.Services;

// Installs/removes the Stop hook in ~/.claude/settings.json by mutating the JSON
// DOM (JsonNode), so every other key -- the UserPromptSubmit hook, permissions,
// plugins -- is preserved untouched.
internal sealed class HookInstallService
{
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    private readonly string _settingsPath;
    private readonly string _command;

    public HookInstallService(string hooksDir)
    {
        var claudeDir = Directory.GetParent(hooksDir)!.FullName;
        _settingsPath = Path.Combine(claudeDir, "settings.json");
        var script = Path.Combine(hooksDir, "speak-response.ps1").Replace('\\', '/');
        _command = $"powershell -NoProfile -ExecutionPolicy Bypass -File {script}";
    }

    public bool IsInstalled()
    {
        var stop = Load()?["hooks"]?["Stop"] as JsonArray;
        return stop is not null && stop.Any(IsOurs);
    }

    public void Install()
    {
        var root = Load() ?? new JsonObject();
        var hooks = root["hooks"] as JsonObject;
        if (hooks is null) { hooks = new JsonObject(); root["hooks"] = hooks; }
        var stop = hooks["Stop"] as JsonArray;
        if (stop is null) { stop = new JsonArray(); hooks["Stop"] = stop; }

        if (!stop.Any(IsOurs))
        {
            stop.Add(new JsonObject
            {
                ["hooks"] = new JsonArray(new JsonObject
                {
                    ["type"] = "command",
                    ["command"] = _command,
                }),
            });
        }
        Save(root);
    }

    public void Uninstall()
    {
        var root = Load();
        if (root?["hooks"] is JsonObject hooks && hooks["Stop"] is JsonArray stop)
        {
            for (var i = stop.Count - 1; i >= 0; i--)
                if (IsOurs(stop[i])) stop.RemoveAt(i);
            if (stop.Count == 0) hooks.Remove("Stop");
            Save(root);
        }
    }

    private static bool IsOurs(JsonNode? entry)
    {
        return entry?["hooks"] is JsonArray hooks
            && hooks.Any(h => (h?["command"]?.GetValue<string>() ?? "").Contains("speak-response.ps1"));
    }

    private JsonObject? Load()
    {
        try { return File.Exists(_settingsPath) ? JsonNode.Parse(File.ReadAllText(_settingsPath)) as JsonObject : null; }
        catch { return null; }
    }

    private void Save(JsonNode root) => File.WriteAllText(_settingsPath, root.ToJsonString(Indented));
}
