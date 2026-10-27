using System.Drawing;
using MaterialSkin.Controls;

namespace unilib.UI.Controls;

public enum ChipKind { Neutral, Accent, Outline }

// Small status label ("3 available", "Pending", "Admin"), the design's .tag.
public class Chip : Label
{
    private ChipKind _kind;

    public Chip(string text, ChipKind kind)
    {
        AutoSize = true;
        Padding = new Padding(8, 3, 8, 3);
        Margin = new Padding(0);
        Font = Theme.Font(11);
        BackColor = Color.Transparent;
        Text = text;
        Kind = kind;
    }

    public ChipKind Kind
    {
        get => _kind;
        set
        {
            _kind = value;
            ForeColor = value switch
            {
                ChipKind.Accent => Theme.Accent800,
                ChipKind.Outline => Theme.Accent,
                _ => Theme.Neutral800
            };
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        if (_kind != ChipKind.Outline)
        {
            using var fill = new SolidBrush(_kind == ChipKind.Accent ? Theme.Accent100 : Theme.Neutral100);
            e.Graphics.FillRectangle(fill, ClientRectangle);
        }
        else
        {
            using var pen = new Pen(Theme.Accent);
            e.Graphics.DrawRectangle(pen, rect);
        }
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

// Flat square button showing one icon, with a hover tint.
public class IconButton : Control
{
    private bool _hover;
    private Glyph _glyph;

    public IconButton(Glyph glyph, string accessibleName, int size = 36)
    {
        _glyph = glyph;
        AccessibleName = accessibleName;
        AccessibleRole = AccessibleRole.PushButton;
        Size = new Size(size, size);
        Cursor = Cursors.Hand;
        IconColor = Theme.Ink;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        BackColor = Color.Transparent;
    }

    public Color IconColor { get; set; }

    public Glyph Glyph
    {
        get => _glyph;
        set { _glyph = value; Invalidate(); }
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        if (_hover)
        {
            using var hover = new SolidBrush(Color.FromArgb(18, Theme.Ink));
            e.Graphics.FillRectangle(hover, ClientRectangle);
        }
        var size = Math.Min(20, Math.Min(Width, Height) - 8);
        Icons.Draw(e.Graphics, _glyph, new Rectangle((Width - size) / 2, (Height - size) / 2, size, size), IconColor);
    }
}

// A full-width row in a side list: the main navigation, the map's library list and
// its shelf list. Optional number on the left, detail text on the right.
public class NavButton : Control
{
    public enum Highlight { Fill, Tint }

    private bool _active;
    private bool _hover;

    public NavButton(string text, string? number = null, string? detail = null)
    {
        Text = text;
        Number = number;
        Detail = detail;
        Height = 56;
        TextFont = Theme.Font(15, FontStyle.Bold);
        Cursor = Cursors.Hand;
        AccessibleRole = AccessibleRole.PushButton;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Ground;
    }

    public string? Number { get; set; }
    public string? Detail { get; set; }
    public int TextIndent { get; set; } = 20;
    public bool ShowRule { get; set; } = true;
    public Font TextFont { get; set; }
    public Highlight Style { get; set; } = Highlight.Fill;

    public bool Active
    {
        get => _active;
        set { _active = value; Invalidate(); }
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Color back, fore, muted;
        if (_active && Style == Highlight.Fill)
            (back, fore, muted) = (Theme.Accent, Theme.Ground, Theme.Ground);
        else if (_active)
            (back, fore, muted) = (Theme.Accent100, Theme.Accent800, Theme.Accent800);
        else
            (back, fore, muted) = (_hover ? Theme.Hover : BackColor, Theme.Ink, Theme.TextMuted);

        g.Clear(back);

        var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis;
        var x = TextIndent;
        if (Number != null)
        {
            var small = Theme.Font(11, FontStyle.Bold);
            var numberWidth = TextRenderer.MeasureText(g, Number, small).Width;
            TextRenderer.DrawText(g, Number, small, new Rectangle(x, 0, numberWidth, Height), muted, flags);
            x += numberWidth + 6;
        }

        var detailWidth = 0;
        if (!string.IsNullOrEmpty(Detail))
        {
            var small = Theme.Font(12, FontStyle.Bold);
            detailWidth = TextRenderer.MeasureText(g, Detail, small).Width + 16;
            TextRenderer.DrawText(g, Detail, small, new Rectangle(Width - detailWidth, 0, detailWidth - 16, Height), muted,
                flags | TextFormatFlags.Right);
        }

        var font = _active || Style == Highlight.Tint ? TextFont : Theme.Font(TextFont.Size, FontStyle.Regular);
        TextRenderer.DrawText(g, Text, font, new Rectangle(x, 0, Width - x - detailWidth - 8, Height), fore, flags);

        if (ShowRule)
        {
            using var rule = new Pen(Theme.Neutral300);
            g.DrawLine(rule, 0, Height - 1, Width, Height - 1);
        }
    }
}

// The design's segmented control (.seg): Material buttons where the selected one is contained.
public class SegmentedButtons : FlowLayoutPanel
{
    private readonly List<MaterialButton> _buttons = new();
    private int _selectedIndex;

    public SegmentedButtons(IEnumerable<string> options, int selectedIndex = 0)
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        WrapContents = false;
        Margin = new Padding(0);
        BackColor = Color.Transparent;

        foreach (var option in options)
        {
            var index = _buttons.Count;
            var button = Ui.Button(option, MaterialButton.MaterialButtonType.Outlined, dense: true);
            button.Margin = new Padding(0, 0, 4, 0);
            button.Click += (s, e) => SelectedIndex = index;
            _buttons.Add(button);
            Controls.Add(button);
        }
        _selectedIndex = selectedIndex;
        UpdateButtons();
    }

    public event EventHandler? SelectedIndexChanged;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value == _selectedIndex)
                return;
            _selectedIndex = value;
            UpdateButtons();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateButtons()
    {
        for (var i = 0; i < _buttons.Count; i++)
            _buttons[i].Type = i == _selectedIndex
                ? MaterialButton.MaterialButtonType.Contained
                : MaterialButton.MaterialButtonType.Outlined;
    }
}
