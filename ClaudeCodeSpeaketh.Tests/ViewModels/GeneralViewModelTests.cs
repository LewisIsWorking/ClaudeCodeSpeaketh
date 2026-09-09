using Xunit;
using ClaudeCodeSpeaketh.Models;
using ClaudeCodeSpeaketh.Services;
using ClaudeCodeSpeaketh.ViewModels;

namespace ClaudeCodeSpeaketh.Tests.ViewModels;

/// <summary>
/// The General tab: the config round trip, and the one piece of behaviour the
/// class defends in a comment but nothing enforced.
/// </summary>
public class GeneralViewModelTests
{
    /// <summary>Records what the view model asked the registry to do, and answers what it is told to.</summary>
    private sealed class FakeStartup : IStartupService
    {
        public bool Enabled { get; init; }
        public bool TrayMode { get; init; } = true;
        public List<(bool Enabled, bool StartInTray)> Writes { get; } = [];

        public bool IsEnabled() => Enabled;
        public bool IsTrayMode() => TrayMode;
        public void SetEnabled(bool enabled, bool startInTray) => Writes.Add((enabled, startInTray));
    }

    // ── the registry write on construction ──────────────────────────────────

    /// <summary>
    /// ⛔ The reason this file exists. The constructor assigns the BACKING FIELDS
    /// (`_startAtStartup = ...`) rather than the properties, precisely so that
    /// reflecting current registry state into the controls does not itself write
    /// back to the registry. Change those two lines to the generated properties
    /// and `OnStartAtStartupChanged` fires during construction, so merely opening
    /// the app rewrites the Run key. Nothing failed when that happened: the value
    /// written is the value just read, so the app behaves identically and only a
    /// registry audit would ever show it.
    /// </summary>
    [Fact]
    public void Constructing_does_not_write_to_the_registry()
    {
        // ⚠️ Enabled MUST be true here. The mutation assigns the property instead of the
        // backing field, and a property assignment only raises OnChanged when the value
        // actually CHANGES. Both default to false, so a fixture returning false would
        // assign false-over-false, fire nothing, and pass while the bug is present.
        var startup = new FakeStartup { Enabled = true, TrayMode = false };

        _ = new GeneralViewModel(startup);

        Assert.Empty(startup.Writes);
    }

    [Fact]
    public void Constructing_reflects_the_current_registration_into_the_controls()
    {
        var vm = new GeneralViewModel(new FakeStartup { Enabled = true, TrayMode = false });

        Assert.True(vm.StartAtStartup);
        Assert.Equal(1, vm.StartupModeIndex); // 1 = window open, 0 = tray
    }

    /// <summary>Index 0 is tray, and that mapping is the whole meaning of the control.</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Changing_the_startup_mode_rewrites_the_registration(int index, bool expectTray)
    {
        var startup = new FakeStartup { Enabled = true };
        var vm = new GeneralViewModel(startup) { StartupModeIndex = index == 0 ? 1 : 0 };
        startup.Writes.Clear();

        vm.StartupModeIndex = index;

        var write = Assert.Single(startup.Writes);
        Assert.Equal(expectTray, write.StartInTray);
    }

    [Fact]
    public void Toggling_start_at_startup_writes_it_through()
    {
        var startup = new FakeStartup { Enabled = false };
        var vm = new GeneralViewModel(startup);

        vm.StartAtStartup = true;

        var write = Assert.Single(startup.Writes);
        Assert.True(write.Enabled);
    }

    // ── the config round trip ───────────────────────────────────────────────

    /// <summary>
    /// Every field LoadFrom reads must survive ApplyTo. A field added to one half
    /// and forgotten in the other loses a setting silently on the next save.
    /// </summary>
    [Fact]
    public void Load_then_apply_preserves_every_field_it_touches()
    {
        var original = new TtsConfig
        {
            Enabled = false,
            Engine = "sapi",
            MaxChars = 800,
            SpeakSessionName = false,
            Sapi = { Rate = -4, Volume = 55 },
        };
        var vm = new GeneralViewModel(new FakeStartup());
        var written = new TtsConfig();

        vm.LoadFrom(original);
        vm.ApplyTo(written);

        Assert.Equal(original.Enabled, written.Enabled);
        Assert.Equal(original.Engine, written.Engine);
        Assert.Equal(original.MaxChars, written.MaxChars);
        Assert.Equal(original.SpeakSessionName, written.SpeakSessionName);
        Assert.Equal(original.Sapi.Rate, written.Sapi.Rate);
        Assert.Equal(original.Sapi.Volume, written.Sapi.Volume);
    }

    /// <summary>
    /// ⚠️ The round trip NORMALISES rather than preserving: the engine is a two-way
    /// toggle, so anything that is not "sapi" reads as neural and writes back as
    /// "edge". A hand-edited config saying "espeak" is silently corrected on the
    /// next save. That is defensible, and it is not obvious, so it is pinned here.
    /// </summary>
    [Theory]
    [InlineData("sapi", "sapi")]
    [InlineData("edge", "edge")]
    [InlineData("espeak", "edge")]
    [InlineData("", "edge")]
    public void An_unrecognised_engine_is_normalised_to_edge(string stored, string expected)
    {
        var vm = new GeneralViewModel(new FakeStartup());
        var written = new TtsConfig();

        vm.LoadFrom(new TtsConfig { Engine = stored });
        vm.ApplyTo(written);

        Assert.Equal(expected, written.Engine);
    }

    /// <summary>
    /// "Speak the entire response" IS `maxChars &lt;= 0`; there is no separate flag in
    /// the config. Both spellings of "no cap" must read the same way.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void No_cap_reads_as_speak_entire_response_and_writes_back_as_zero(int stored)
    {
        var vm = new GeneralViewModel(new FakeStartup());
        var written = new TtsConfig { MaxChars = 999 };

        vm.LoadFrom(new TtsConfig { MaxChars = stored });

        Assert.True(vm.SpeakEntireResponse);
        // ⚠️ NOT 0. The slider needs a usable number for when the box is unticked,
        // so an absent cap loads as the 1500 default rather than as nothing.
        Assert.Equal(1500, vm.MaxCharsValue);

        vm.ApplyTo(written);
        Assert.Equal(0, written.MaxChars);
    }

    [Fact]
    public void A_cap_is_kept_and_disables_speak_entire_response()
    {
        var vm = new GeneralViewModel(new FakeStartup());

        vm.LoadFrom(new TtsConfig { MaxChars = 250 });

        Assert.False(vm.SpeakEntireResponse);
        Assert.Equal(250, vm.MaxCharsValue);
    }

    /// <summary>The cap box is enabled exactly when the whole response is not being read.</summary>
    [Fact]
    public void The_cap_is_editable_only_when_not_speaking_the_entire_response()
    {
        var vm = new GeneralViewModel(new FakeStartup()) { SpeakEntireResponse = true };
        Assert.False(vm.MaxCharsEnabled);

        vm.SpeakEntireResponse = false;
        Assert.True(vm.MaxCharsEnabled);
    }
}
