using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace unilib.UI.Controls;

// A library floor plan: two rows of six shelves, the entrance, and a dashed route
// from the entrance to the selected shelf. Drawn on a 700x460 grid scaled to fit.
public class FloorPlanPanel : Control
{
    private const float PlanWidth = 700;
    private const float PlanHeight = 460;
    private const int Pad = 16;

    private static readonly Dictionary<string, RectangleF> Shelves = Shelving.Codes.ToDictionary(
        code => code,
        code => new RectangleF(100 + (code[1] - '1') * 72, code[0] == 'A' ? 80 : 240, 28, 100));

    private string? _target;

    public FloorPlanPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Surface;
    }

    // Raised when a shelf is clicked, with its code ("A3").
    public event EventHandler<string>? ShelfClicked;

    public HashSet<string> Occupied { get; set; } = new();
    public string RoomLabel { get; set; } = "READING ROOM";

    public string? Target
    {
        get => _target;
        set { _target = value; Invalidate(); }
    }

    private (float Scale, PointF Offset) Fit()
    {
        var scale = Math.Min((Width - 2 * Pad) / PlanWidth, (Height - 2 * Pad) / PlanHeight);
        return (Math.Max(scale, 0.01f), new PointF(Pad, Pad));
    }

    private string? ShelfAt(Point location)
    {
        var (scale, offset) = Fit();
        var p = new PointF((location.X - offset.X) / scale, (location.Y - offset.Y) / scale);
        return Shelves.FirstOrDefault(s => s.Value.Contains(p)).Key;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        Cursor = ShelfAt(e.Location) != null ? Cursors.Hand : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (ShelfAt(e.Location) is { } code)
            ShelfClicked?.Invoke(this, code);
        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        var (scale, offset) = Fit();
        g.TranslateTransform(offset.X, offset.Y);
        g.ScaleTransform(scale, scale);

        using var ink = new SolidBrush(Theme.Ink);
        using var muted = new SolidBrush(Theme.TextMuted);
        using var accent = new SolidBrush(Theme.Accent);
        using var wall = new Pen(Theme.Ink, 3);

        // Outer walls with the entrance gap on the left, and the inner wall of the side room.
        g.DrawLines(wall, new PointF[] { new(40, 360), new(40, 40), new(660, 40), new(660, 420), new(40, 420), new(40, 400) });
        g.DrawLine(wall, 540, 40, 540, 300);
        g.DrawString(RoomLabel, Theme.Font(11), muted, 556, 60);
        g.FillRectangle(ink, 34, 374, 12, 12);
        g.DrawString("ENTRANCE", Theme.Font(11), ink, 38, 432);

        var label = Theme.Font(12);
        var labelBold = Theme.Font(12, FontStyle.Bold);
        using var center = new StringFormat { Alignment = StringAlignment.Center };
        foreach (var (code, rect) in Shelves)
        {
            var isTarget = code == _target;
            var fill = isTarget ? Theme.Accent : Occupied.Contains(code) ? Theme.Neutral400 : Theme.Ground;
            using (var brush = new SolidBrush(fill))
                g.FillRectangle(brush, rect);
            using (var edge = new Pen(isTarget ? Theme.Accent : Theme.Ink, 1.5f))
                g.DrawRectangle(edge, rect.X, rect.Y, rect.Width, rect.Height);
            using var text = new SolidBrush(isTarget ? Theme.Accent700 : Theme.TextMuted);
            g.DrawString(code, isTarget ? labelBold : label, text, rect.X + rect.Width / 2, rect.Y - 22, center);
        }

        if (_target != null && Shelves.TryGetValue(_target, out var shelf))
        {
            var cx = shelf.X + shelf.Width / 2;
            var route = _target[0] == 'A'
                ? new PointF[] { new(40, 380), new(70, 380), new(70, 210), new(cx, 210), new(cx, 192) }
                : new PointF[] { new(40, 380), new(cx, 380), new(cx, 352) };
            using var dashed = new Pen(Theme.Accent, 3) { DashPattern = new[] { 3f, 2f }, LineJoin = LineJoin.Round };
            g.DrawLines(dashed, route);

            var end = route[^1];
            g.FillEllipse(accent, end.X - 8, end.Y - 8, 16, 16);
            using var ring = new Pen(Theme.Surface, 3);
            g.DrawEllipse(ring, end.X - 8, end.Y - 8, 16, 16);
        }
    }
}
