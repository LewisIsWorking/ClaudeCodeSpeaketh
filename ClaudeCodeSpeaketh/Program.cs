using System;
using Avalonia;

namespace ClaudeCodeSpeaketh;

// Entry point. Kept minimal on purpose -- Velopack auto-update can be added
// later when this app starts shipping releases (mirrors the sibling launchers).
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
