using System;
using System.IO;
using System.Linq;

namespace ClaudeCodeSpeaketh.Services;

// Reads the bundled CHANGELOG.md (an embedded resource) for display on the
// Updates tab. Offline and reliable -- no dependency on GitHub release notes.
internal sealed class ChangelogService
{
    public string Load()
    {
        var asm = typeof(ChangelogService).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("CHANGELOG.md", StringComparison.OrdinalIgnoreCase));
        if (name is null) return "Changelog unavailable.";

        using var stream = asm.GetManifestResourceStream(name);
        if (stream is null) return "Changelog unavailable.";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
