namespace ClaudeCodeSpeaketh.Services;

// Controls whether the app launches automatically when the user signs in to
// Windows. Source of truth is the registry Run key, not tts-config.json.
internal interface IStartupService
{
    /// <summary>True if the app is currently registered to launch at sign-in.</summary>
    bool IsEnabled();

    /// <summary>Add (or remove) the launch-at-sign-in registration.</summary>
    void SetEnabled(bool enabled);
}
