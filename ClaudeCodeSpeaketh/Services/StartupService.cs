using System;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ClaudeCodeSpeaketh.Services;

// Registers the app under HKCU\...\CurrentVersion\Run so Windows launches it at
// sign-in. HKCU (not HKLM) means no admin prompt. The stored command carries the
// --startup flag so the app comes up hidden in the tray (App.axaml.cs honours it)
// rather than popping its window on every login.
[SupportedOSPlatform("windows")]
internal sealed class StartupService : IStartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClaudeCodeSpeaketh";
    private const string StartupArg = "--startup";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) is string;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null) return;

        if (enabled)
        {
            // Environment.ProcessPath is the running .exe (the stable Velopack
            // 'current' path once installed); quote it for spaces in the path.
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe)) return;
            key.SetValue(ValueName, $"\"{exe}\" {StartupArg}");
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
