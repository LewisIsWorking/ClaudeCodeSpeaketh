using System;
using System.IO;
using System.Text.Json;
using ClaudeCodeSpeaketh.Models;

namespace ClaudeCodeSpeaketh.Services;

internal interface IConfigService
{
    string ConfigPath { get; }
    string HooksDir { get; }
    TtsConfig Load();
    void Save(TtsConfig config);
}

// Reads/writes ~/.claude/hooks/tts-config.json (the contract the PowerShell hook
// consumes). Corrupt-tolerant: a bad file falls back to defaults rather than throwing.
internal sealed class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public string HooksDir { get; }
    public string ConfigPath { get; }

    public ConfigService()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        HooksDir = Path.Combine(home, ".claude", "hooks");
        ConfigPath = Path.Combine(HooksDir, "tts-config.json");
    }

    public TtsConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var cfg = JsonSerializer.Deserialize<TtsConfig>(json, JsonOptions);
                if (cfg is not null) return cfg;
            }
        }
        catch (JsonException) { /* fall through to defaults */ }
        catch (IOException) { }

        return new TtsConfig();
    }

    public void Save(TtsConfig config)
    {
        config.UpdatedBy = "ClaudeCodeSpeaketh";
        config.UpdatedUtc = DateTime.UtcNow.ToString("o");
        Directory.CreateDirectory(HooksDir);
        // WriteAllText => UTF-8 without BOM, which the PowerShell reader expects.
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }
}
