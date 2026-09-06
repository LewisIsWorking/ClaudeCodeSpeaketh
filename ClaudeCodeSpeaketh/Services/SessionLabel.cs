using System.IO;

namespace ClaudeCodeSpeaketh.Services;

// How a Claude session is named for humans -- shared by the Sessions tab and the
// spoken announcement so the list and the voice never disagree.
//
// Claude Code does not expose the terminal tab title to a hook, so the best
// available stand-in is the session working directory: the project-folder leaf is
// what a terminal tab is almost always named after ("ComeOnOverUno"), and it is
// what the Stop hook already reports as `cwd`.
internal static class SessionLabel
{
    /// <summary>Project-folder leaf of a session cwd, or "" when unknown.</summary>
    public static string Leaf(string cwd) =>
        string.IsNullOrWhiteSpace(cwd)
            ? ""
            : Path.GetFileName(cwd.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    /// <summary>
    /// The name to speak before an utterance. Falls back to a short session id so
    /// an unknown cwd still tells one terminal from another, and returns "" only
    /// when there is nothing useful to say.
    /// </summary>
    public static string ForSpeech(string cwd, string sessionId)
    {
        var leaf = Leaf(cwd);
        if (leaf.Length > 0) return leaf;
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId == "unknown") return "";
        return sessionId.Length > 8 ? sessionId[..8] : sessionId;
    }
}
