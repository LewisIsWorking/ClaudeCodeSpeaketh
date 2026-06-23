using Avalonia;
using Avalonia.Controls;

namespace ClaudeCodeSpeaketh.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    // Closing the window hides it (the speech daemon keeps running in the tray)
    // unless the user chose Quit from the tray menu.
    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (Application.Current is App { Exiting: false })
        {
            e.Cancel = true;
            Hide();
        }
    }
}
