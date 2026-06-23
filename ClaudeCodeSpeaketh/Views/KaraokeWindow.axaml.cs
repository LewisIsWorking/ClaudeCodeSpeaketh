using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace ClaudeCodeSpeaketh.Views;

// Always-on-top companion window: shows the response and highlights each word as
// it is spoken. Driven by EdgeKaraokePlayer on the UI thread.
public partial class KaraokeWindow : Window
{
    private static readonly IBrush Dim = new SolidColorBrush(Color.Parse("#9aa0a6"));
    private readonly List<Run> _runs = new();
    private int _current = -1;
    private Color _hlColor;
    private IBrush? _hlBrush;

    public KaraokeWindow() => InitializeComponent();

    public void SetWords(IReadOnlyList<string> words)
    {
        _runs.Clear();
        _current = -1;
        WordsBlock.Inlines!.Clear();
        foreach (var w in words)
        {
            var run = new Run(w + " ") { Foreground = Dim };
            _runs.Add(run);
            WordsBlock.Inlines.Add(run);
        }
    }

    // Takes a Color (thread-safe struct); the brush is built here on the UI
    // thread -- a Brush created off-thread crashes the compositor.
    public void Highlight(int index, Color color)
    {
        if (index == _current || index < 0 || index >= _runs.Count) return;
        if (_hlBrush is null || color != _hlColor) { _hlColor = color; _hlBrush = new SolidColorBrush(color); }
        if (_current >= 0 && _current < _runs.Count)
        {
            _runs[_current].Foreground = Dim;
            _runs[_current].FontWeight = FontWeight.Normal;
        }
        _runs[index].Foreground = _hlBrush;
        _runs[index].FontWeight = FontWeight.Bold;
        _current = index;
    }
}
