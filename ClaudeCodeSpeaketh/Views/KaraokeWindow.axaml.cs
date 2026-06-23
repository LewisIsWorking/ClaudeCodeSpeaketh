using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
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

    private string _position = "Center";

    public KaraokeWindow()
    {
        InitializeComponent();
        // Borderless window has no title bar to grab: let the user drag it by
        // pressing anywhere on the overlay. BeginMoveDrag hands off to the OS.
        Cursor = new Cursor(StandardCursorType.SizeAll);
        PointerPressed += OnPointerPressed;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    // Font size + screen position. Position is applied once the window is sized.
    public void Configure(int fontSize, string position)
    {
        if (fontSize > 0) WordsBlock.FontSize = fontSize;
        _position = position;
        Opened += (_, _) => ApplyPosition();
    }

    private void ApplyPosition()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;
        var area = screen.WorkingArea;          // device pixels
        var w = (int)(ClientSize.Width * RenderScaling);
        var h = (int)(ClientSize.Height * RenderScaling);
        var x = area.X + (area.Width - w) / 2;  // always horizontally centred
        var margin = (int)(40 * RenderScaling);
        var y = _position switch
        {
            "Top" => area.Y + margin,
            "Bottom" => area.Y + area.Height - h - margin,
            _ => area.Y + (area.Height - h) / 2,
        };
        Position = new PixelPoint(x, y);
    }

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
