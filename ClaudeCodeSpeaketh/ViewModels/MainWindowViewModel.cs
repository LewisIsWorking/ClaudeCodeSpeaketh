using CommunityToolkit.Mvvm.ComponentModel;

namespace ClaudeCodeSpeaketh.ViewModels;

// Composition root for the window. Phase 0 is a stub; Phase 1 wires in the
// config/voice services and the sub-ViewModels (General / SAPI / Piper / Hook).
internal partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _heading = "ClaudeCodeSpeaketh";

    [ObservableProperty]
    private string _subheading = "Configure how Claude Code reads its responses aloud.";
}
