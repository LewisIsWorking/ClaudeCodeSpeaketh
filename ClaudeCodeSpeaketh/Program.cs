using System;
using System.IO;
using Avalonia;
using Velopack;

namespace ClaudeCodeSpeaketh;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Last-resort crash log so a background-thread exception leaves a trace.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "hooks");
                File.WriteAllText(Path.Combine(dir, "ccs-crash.log"), e.ExceptionObject?.ToString());
            }
            catch { }
        };

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
