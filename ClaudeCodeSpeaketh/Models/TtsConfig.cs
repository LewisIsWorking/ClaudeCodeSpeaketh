using System.Collections.Generic;

namespace ClaudeCodeSpeaketh.Models;

// Serializable mirror of ~/.claude/hooks/tts-config.json -- the single contract
// shared with the PowerShell hook (tts-config.ps1). Serialized camelCase.
internal sealed class TtsConfig
{
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Master on/off for spoken responses.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>"edge" (neural, default) or "sapi" (classic, offline).</summary>
    public string Engine { get; set; } = "edge";

    public SapiSettings Sapi { get; set; } = new();
    public EdgeSettings Edge { get; set; } = new();
    public KaraokeSettings Karaoke { get; set; } = new();

    /// <summary>0 (or negative) = ALL: speak the entire response. &gt;0 = cap.</summary>
    public int MaxChars { get; set; }

    public bool StripMarkdown { get; set; } = true;

    /// <summary>Queue across different Claude sessions (vs. only the latest).</summary>
    public bool QueueAcrossSessions { get; set; } = true;

    /// <summary>Per-session mute: sessionId -> enabled. Absent = enabled.</summary>
    public Dictionary<string, bool> SessionOverrides { get; set; } = new();

    public string? UpdatedBy { get; set; }
    public string? UpdatedUtc { get; set; }
}

internal sealed class SapiSettings
{
    public string Voice { get; set; } = "Microsoft Hazel Desktop";
    public int Rate { get; set; }          // -10..10
    public int Volume { get; set; } = 100; // 0..100
}

internal sealed class EdgeSettings
{
    // Default fresh-install voice: free female Irish English neural voice.
    public string Voice { get; set; } = "en-IE-EmilyNeural";
}

internal sealed class KaraokeSettings
{
    // Show the companion window that highlights each word as it's spoken
    // (resident-app + edge engine only). On by default.
    public bool Enabled { get; set; } = true;
    public string ColorHex { get; set; } = "#FFD54A"; // amber highlight
}
