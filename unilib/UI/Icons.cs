using System.Drawing;
using System.Drawing.Drawing2D;

namespace unilib.UI;

public enum Glyph { Menu, Search, Bookmark, BookmarkFilled, Pin, Plus, LogOut, Eye, EyeOff, ArrowRight }

// Line icons in the style of Lucide (the design's icon set), drawn on a 24x24 grid.
public static class Icons
{
    public static void Draw(Graphics g, Glyph glyph, Rectangle bounds, Color color)
    {
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(bounds.X, bounds.Y);
        g.ScaleTransform(bounds.Width / 24f, bounds.Height / 24f);

        using var pen = new Pen(color, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(color);

        switch (glyph)
        {
            case Glyph.Menu:
                g.DrawLine(pen, 4, 6, 20, 6);
                g.DrawLine(pen, 4, 12, 20, 12);
                g.DrawLine(pen, 4, 18, 20, 18);
                break;
            case Glyph.Search:
                g.DrawEllipse(pen, 3, 3, 16, 16);
                g.DrawLine(pen, 21, 21, 16.65f, 16.65f);
                break;
            case Glyph.Bookmark:
            case Glyph.BookmarkFilled:
                var mark = new[] { new PointF(5, 3), new PointF(19, 3), new PointF(19, 21), new PointF(12, 17), new PointF(5, 21) };
                if (glyph == Glyph.BookmarkFilled)
                    g.FillPolygon(brush, mark);
                g.DrawPolygon(pen, mark);
                break;
            case Glyph.Pin:
                using (var path = new GraphicsPath())
                {
                    path.AddArc(4, 2, 16, 16, 150, 240);
                    path.AddLine(18.93f, 14, 12, 22);
                    path.AddLine(12, 22, 5.07f, 14);
                    path.CloseFigure();
                    g.DrawPath(pen, path);
                }
                g.DrawEllipse(pen, 9, 7, 6, 6);
                break;
            case Glyph.Plus:
                g.DrawLine(pen, 5, 12, 19, 12);
                g.DrawLine(pen, 12, 5, 12, 19);
                break;
            case Glyph.LogOut:
                g.DrawLines(pen, new[] { new PointF(9, 3), new PointF(5, 3), new PointF(5, 21), new PointF(9, 21) });
                g.DrawLine(pen, 21, 12, 9, 12);
                g.DrawLines(pen, new[] { new PointF(16, 7), new PointF(21, 12), new PointF(16, 17) });
                break;
            case Glyph.Eye:
            case Glyph.EyeOff:
                using (var path = new GraphicsPath())
                {
                    path.AddBezier(2, 12, 6, 4, 18, 4, 22, 12);
                    path.AddBezier(22, 12, 18, 20, 6, 20, 2, 12);
                    g.DrawPath(pen, path);
                }
                g.DrawEllipse(pen, 9, 9, 6, 6);
                if (glyph == Glyph.EyeOff)
                    g.DrawLine(pen, 3, 3, 21, 21);
                break;
            case Glyph.ArrowRight:
                g.DrawLine(pen, 5, 12, 19, 12);
                g.DrawLines(pen, new[] { new PointF(12, 5), new PointF(19, 12), new PointF(12, 19) });
                break;
        }

        g.Restore(state);
    }

    // For MaterialSkin controls that take an Image (button and text field icons).
    public static Bitmap Bitmap(Glyph glyph, Color color, int size = 24)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        Draw(g, glyph, new Rectangle(0, 0, size, size), color);
        return bmp;
    }
}
