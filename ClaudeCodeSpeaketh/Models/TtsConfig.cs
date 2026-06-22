namespace ClaudeCodeSpeaketh.Models;

// Serializable mirror of ~/.claude/hooks/tts-config.json -- the single contract
// shared with the PowerShell hook (tts-config.ps1). Serialized camelCase.
internal sealed class TtsConfig
{
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Master on/off for spoken responses.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>"sapi" or "piper" -- the default engine.</summary>
    public string Engine { get; set; } = "sapi";

    public SapiSettings Sapi { get; set; } = new();
    public PiperSettings Piper { get; set; } = new();

    /// <summary>0 (or negative) = ALL: speak the entire response. &gt;0 = cap.</summary>
    public int MaxChars { get; set; }

    public bool StripMarkdown { get; set; } = true;

    public string? UpdatedBy { get; set; }
    public string? UpdatedUtc { get; set; }
}

internal sealed class SapiSettings
{
    public string Voice { get; set; } = "Microsoft Hazel Desktop";
    public int Rate { get; set; }          // -10..10
    public int Volume { get; set; } = 100; // 0..100
}

internal sealed class PiperSettings
{
    public string ExePath { get; set; } = "";
    public string ModelPath { get; set; } = "";
    public string ModelKey { get; set; } = "";
    public double LengthScale { get; set; } = 1.0;
    public bool UseCuda { get; set; } = true;
}
