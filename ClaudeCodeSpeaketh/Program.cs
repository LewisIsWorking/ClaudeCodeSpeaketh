using System;
using Avalonia;
using Velopack;

namespace ClaudeCodeSpeaketh;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // MUST be the very first thing in Main: Velopack re-invokes this exe for
        // its install/update/uninstall hooks, and the handler must short-circuit
        // before any Avalonia window flashes up.
        VelopackApp.Build().Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
