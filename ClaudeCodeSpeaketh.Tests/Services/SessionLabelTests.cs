using ClaudeCodeSpeaketh.Services;
using Xunit;

namespace ClaudeCodeSpeaketh.Tests.Services;

/// <summary>
/// SessionLabel decides what the voice announces before each response and what
/// the Sessions tab lists, so a change here is audible. It is also the only
/// fully pure module in the app, which is why it is the first thing covered.
/// </summary>
public class SessionLabelTests
{
    // ------------------------------------------------------------- Leaf

    [Theory]
    [InlineData(@"C:\Users\Lewis\RiderProjects\ComeOnOverUno", "ComeOnOverUno")]
    [InlineData(@"C:\Users\Lewis\RiderProjects\ChevronListsVsCode", "ChevronListsVsCode")]
    [InlineData("/home/lewis/projects/eel-sea", "eel-sea")]
    public void Leaf_returns_the_project_folder_name(string cwd, string expected)
    {
        Assert.Equal(expected, SessionLabel.Leaf(cwd));
    }

    [Theory]
    [InlineData(@"C:\Users\Lewis\RiderProjects\ComeOnOverUno\")]
    [InlineData("/home/lewis/projects/eel-sea/")]
    public void Leaf_ignores_a_trailing_separator(string cwd)
    {
        // Without the TrimEnd this returns "" -- a terminal that announced
        // nothing at all, which is the failure users would actually notice.
        Assert.NotEqual("", SessionLabel.Leaf(cwd));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Leaf_is_empty_for_a_missing_cwd(string? cwd)
    {
        Assert.Equal("", SessionLabel.Leaf(cwd!));
    }

    // --------------------------------------------------------- ForSpeech

    [Fact]
    public void ForSpeech_prefers_the_folder_name()
    {
        Assert.Equal(
            "ComeOnOverUno",
            SessionLabel.ForSpeech(@"C:\Users\Lewis\RiderProjects\ComeOnOverUno", "abc123def456"));
    }

    [Fact]
    public void ForSpeech_falls_back_to_a_short_session_id()
    {
        // Two anonymous terminals must still be distinguishable by ear.
        Assert.Equal("abc123de", SessionLabel.ForSpeech("", "abc123def456"));
    }

    [Fact]
    public void ForSpeech_keeps_a_session_id_shorter_than_the_cut()
    {
        Assert.Equal("abc", SessionLabel.ForSpeech("", "abc"));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("", "   ")]
    [InlineData("", "unknown")]
    public void ForSpeech_is_empty_when_there_is_nothing_useful_to_say(string cwd, string sessionId)
    {
        // "unknown" is what the Stop hook writes when Claude Code sends no
        // session_id; speaking it aloud before every response would be worse
        // than saying nothing.
        Assert.Equal("", SessionLabel.ForSpeech(cwd, sessionId));
    }
}
