namespace ClaudeCodeSpeaketh.Services;

// Controls whether the app launches automatically when the user signs in to
// Windows. Source of truth is the registry Run key, not tts-config.json.
internal interface IStartupService
{
    /// <summary>True if the app is currently registered to launch at sign-in.</summary>
    bool IsEnabled();

    /// <summary>True if the stored launch starts hidden in the tray (vs. window).</summary>
    bool IsTrayMode();

    /// <summary>
    /// Add (or remove) the launch-at-sign-in registration. When enabled,
    /// <paramref name="startInTray"/> chooses hidden-in-tray vs. window-open.
    /// </summary>
    void SetEnabled(bool enabled, bool startInTray);
}
