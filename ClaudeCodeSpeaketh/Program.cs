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
        //
        // Redeploy the embedded hook scripts the instant Velopack installs or
        // updates the app. The new version's speech-cleaning fixes then take effect
        // on Claude Code's NEXT turn, without waiting for the user to open the app
        // (the on-startup DeployAll in MainWindowViewModel remains a backstop).
        // These fast callbacks run in a short-lived hook invocation against the
        // already-swapped new assembly, so DeployAll writes the NEW scripts.
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => RedeployHooks())
            .OnAfterUpdateFastCallback(_ => RedeployHooks())
            .Run();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Rewrites ~/.claude/hooks from this exe's embedded resources. Best-effort:
    // a Velopack hook callback must never throw (it would abort the update).
    private static void RedeployHooks()
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var hooksDir = Path.Combine(home, ".claude", "hooks");
            new Services.HookScriptDeployService(hooksDir).DeployAll();
        }
        catch { /* startup DeployAll is the backstop */ }
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
