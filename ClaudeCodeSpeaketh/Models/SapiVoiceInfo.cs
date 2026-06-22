namespace ClaudeCodeSpeaketh.Models;

// A classic SAPI voice as seen by System.Speech. Display-friendly for the picker.
internal sealed class SapiVoiceInfo
{
    public required string Name { get; init; }
    public string Culture { get; init; } = "";
    public string Gender { get; init; } = "";

    // e.g. "Microsoft Hazel Desktop  -  en-GB  (Female)"
    public string Display =>
        Culture.Length == 0 ? Name : $"{Name}  -  {Culture}  ({Gender})";
}
