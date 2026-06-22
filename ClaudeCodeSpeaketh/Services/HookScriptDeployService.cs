using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ClaudeCodeSpeaketh.Services;

// Writes the refactored PowerShell hook scripts (embedded in this exe) into
// ~/.claude/hooks, guaranteeing pure-ASCII output -- Windows PowerShell 5.1
// reads BOM-less files as ANSI and would corrupt any non-ASCII byte.
internal sealed class HookScriptDeployService
{
    private static readonly string[] ScriptNames =
    {
        "tts-config.ps1",
        "speak-response.ps1",
        "tts-speaker.ps1",
    };

    private readonly string _hooksDir;

    public HookScriptDeployService(string hooksDir) => _hooksDir = hooksDir;

    /// <summary>Deploys every embedded hook script. Returns the names written.</summary>
    public string[] DeployAll()
    {
        Directory.CreateDirectory(_hooksDir);
        var asm = Assembly.GetExecutingAssembly();
        var written = ScriptNames.Where(name => DeployOne(asm, name)).ToArray();
        return written;
    }

    private bool DeployOne(Assembly asm, string scriptName)
    {
        var resource = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("." + scriptName, StringComparison.OrdinalIgnoreCase)
                              || n.EndsWith(scriptName, StringComparison.OrdinalIgnoreCase));
        if (resource is null) return false;

        using var stream = asm.GetManifestResourceStream(resource);
        if (stream is null) return false;

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        AssertAscii(content, scriptName);

        var target = Path.Combine(_hooksDir, scriptName);
        File.WriteAllText(target, content, new ASCIIEncoding());
        return true;
    }

    private static void AssertAscii(string content, string scriptName)
    {
        foreach (var c in content)
        {
            if (c > 0x7E && c != '\r' && c != '\n' && c != '\t')
                throw new InvalidDataException(
                    $"Hook script '{scriptName}' contains non-ASCII char U+{(int)c:X4}; " +
                    "PowerShell 5.1 would corrupt it. Keep hook scripts pure ASCII.");
        }
    }
}
