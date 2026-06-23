using System;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using ClaudeCodeSpeaketh.ViewModels;
using ClaudeCodeSpeaketh.Views;

namespace ClaudeCodeSpeaketh;

// The whole app is Windows-only (System.Speech + speech registry).
[SupportedOSPlatform("windows")]
public partial class App : Application
{
    private MainWindowViewModel? _vm;
    private MainWindow? _window;

    // True only when the user really wants to quit (tray Quit) -- otherwise
    // closing the window just hides it so the speech daemon stays resident.
    internal bool Exiting { get; private set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _vm = new MainWindowViewModel();
            _window = new MainWindow { DataContext = _vm };

            // Keep running in the tray when the window is closed.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.ShutdownRequested += (_, _) => _vm?.Dispose();

            // Launched at sign-in (--startup): come up hidden in the tray. Leaving
            // MainWindow unset means the framework shows nothing; the tray's Open
            // reveals the window on demand.
            var startHidden = desktop.Args is { } args && Array.IndexOf(args, "--startup") >= 0;
            if (!startHidden) desktop.MainWindow = _window;

            SetUpTray(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetUpTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            using var s = AssetLoader.Open(new Uri("avares://ClaudeCodeSpeaketh/Assets/app.ico"));
            var tray = new TrayIcon { Icon = new WindowIcon(s), ToolTipText = "ClaudeCodeSpeaketh", IsVisible = true };

            var menu = new NativeMenu();
            var open = new NativeMenuItem("Open");
            open.Click += (_, _) => ShowWindow();
            var quit = new NativeMenuItem("Quit");
            quit.Click += (_, _) => { Exiting = true; _vm?.Dispose(); desktop.Shutdown(); };
            menu.Add(open);
            menu.Add(quit);
            tray.Menu = menu;
            tray.Clicked += (_, _) => ShowWindow();

            TrayIcon.SetIcons(this, new TrayIcons { tray });
        }
        catch
        {
            // No tray (icon missing etc.): fall back to quitting on window close,
            // and make sure the window is visible -- otherwise a --startup launch
            // would leave no window and no tray (invisible, unquittable).
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.MainWindow = _window;
            ShowWindow();
        }
    }

    private void ShowWindow()
    {
        if (_window is null) return;
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }
}
