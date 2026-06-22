using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Speech.Synthesis;
using ClaudeCodeSpeaketh.Models;

namespace ClaudeCodeSpeaketh.Services;

// Enumerates the classic SAPI voices visible to System.Speech -- the same set the
// hook's SAPI path can actually select.
[SupportedOSPlatform("windows")]
internal sealed class SapiVoiceService
{
    public IReadOnlyList<SapiVoiceInfo> GetInstalledVoices()
    {
        var list = new List<SapiVoiceInfo>();
        using var synth = new SpeechSynthesizer();
        foreach (var v in synth.GetInstalledVoices())
        {
            if (!v.Enabled) continue;
            var info = v.VoiceInfo;
            list.Add(new SapiVoiceInfo
            {
                Name = info.Name,
                Culture = info.Culture?.Name ?? "",
                Gender = info.Gender.ToString(),
            });
        }
        return list;
    }
}
