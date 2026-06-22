namespace ClaudeCodeSpeaketh.Models;

// A neural voice offered by edge-tts (e.g. en-IE-EmilyNeural).
internal sealed class EdgeVoiceInfo
{
    public required string ShortName { get; init; }   // e.g. en-IE-EmilyNeural
    public string Gender { get; init; } = "";
    public string Locale { get; init; } = "";          // e.g. en-IE

    // "en-IE  EmilyNeural  (Female)"
    public string Display => $"{Locale}  {ShortName.Substring(Locale.Length + 1)}  ({Gender})";
}
