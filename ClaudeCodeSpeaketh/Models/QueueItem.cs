namespace ClaudeCodeSpeaketh.Models;

// One queued utterance dropped by the Stop hook into ~/.claude/hooks/tts-queue/.
internal sealed class QueueItem
{
    public string SessionId { get; set; } = "";
    public string Text { get; set; } = "";
}
