using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;

namespace ClaudeCodeSpeaketh.Views;

// Always-on-top companion window: shows the response and highlights each word as
// it is spoken. Driven by the karaoke players on the UI thread; its transport
// buttons bind to the shared PlaybackController (set via SetController).
public partial class KaraokeWindow : Window
{
    private static readonly IBrush Dim = new SolidColorBrush(Color.Parse("#9aa0a6"));

    // A new window is created per utterance, so remember the user's size/position
    // for the session and restore it -- otherwise every response would reset it.
    private static PixelPoint? _savedPos;
    private static Size? _savedSize;

    private readonly List<Run> _runs = new();
    private int _current = -1;
    private Color _hlColor;
    private IBrush? _hlBrush;

    private string _position = "Center";
    private bool _track;   // capture size/position changes only after first layout

    public KaraokeWindow()
    {
        InitializeComponent();
        // Drag the window by pressing the text area (the buttons handle their own
        // clicks); drag the corner grip to resize.
        WordsScroll.Cursor = new Cursor(StandardCursorType.SizeAll);
        WordsScroll.PointerPressed += OnDragPressed;
        ResizeGrip.PointerPressed += OnGripPressed;

        PositionChanged += (_, _) => { if (_track) _savedPos = Position; };
    }

    // Capture user resizes (the corner grip) so the next utterance's window keeps
    // the chosen size. Runs on every layout pass; guarded until first positioning.
    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        if (_track) _savedSize = result;
        return result;
    }

    // Receives the PlaybackController (typed as object to keep this public API free
    // of the internal type); its public command/state members bind by reflection.
    public void SetController(object controller) => DataContext = controller;

    private void OnDragPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginResizeDrag(WindowEdge.SouthEast, e);
            e.Handled = true;
        }
    }

    // Font size + screen position. Restores the session's size first, then
    // positions the window once it is sized.
    public void Configure(int fontSize, string position)
    {
        if (fontSize > 0) WordsBlock.FontSize = fontSize;
        _position = position;
        if (_savedSize is { } s) { Width = s.Width; Height = s.Height; }
        Opened += (_, _) => ApplyPosition();
    }

    private void ApplyPosition()
    {
        // If the user moved the overlay this session, honour that over the setting.
        if (_savedPos is { } p) { Position = p; _track = true; return; }

        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) { _track = true; return; }
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
        _track = true;
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
