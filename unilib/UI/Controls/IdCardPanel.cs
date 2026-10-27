using System.Drawing;
using System.Drawing.Text;
using unilib.Models;

namespace unilib.UI.Controls;

// The digital library card: ink title band, initials block, name, ID and a barcode.
public class IdCardPanel : Control
{
    private readonly User _user;

    public IdCardPanel(User user)
    {
        _user = user;
        Size = new Size(450, 262);
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Ground;
    }

    // Students show their student ID; other accounts their library account number.
    public static string CardNumber(User user) => user.StudentId ?? $"UL{user.Id:D6}";

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Theme.Ground);
        using var ink = new SolidBrush(Theme.Ink);
        using var ground = new SolidBrush(Theme.Ground);
        using var muted = new SolidBrush(Theme.TextMuted);

        using (var border = new Pen(Theme.Ink, 2))
            g.DrawRectangle(border, 1, 1, Width - 2, Height - 2);

        // Title band.
        g.FillRectangle(ink, 0, 0, Width, 40);
        g.DrawString("UniLib Student ID", Theme.Font(15, FontStyle.Bold), ground, 14, 10);
        var role = Ui.RoleLabel(_user).ToUpperInvariant();
        var roleFont = Theme.Font(11);
        var roleWidth = g.MeasureString(role, roleFont).Width;
        g.DrawString(role, roleFont, ground, Width - 16 - roleWidth, 13);

        // Photo placeholder with initials.
        var photo = new Rectangle(16, 60, 96, 120);
        using (var photoFill = new SolidBrush(Theme.Neutral300))
            g.FillRectangle(photoFill, photo);
        var initials = $"{Initial(_user.FirstName)}{Initial(_user.LastName)}";
        g.DrawString(initials, Theme.Font(30, FontStyle.Bold), ink, photo.X + 4, photo.Bottom - 44);

        var textX = photo.Right + 20;
        var textWidth = Width - textX - 16;
        g.DrawString($"{_user.FirstName} {_user.LastName}", Theme.Font(26, FontStyle.Bold), ink,
            new RectangleF(textX, 58, textWidth, 64));
        g.DrawString($"ID: {CardNumber(_user)}", Theme.Font(14), ink, textX, 126);
        g.DrawString(_user.Email, Theme.Font(14), muted, new RectangleF(textX, 148, textWidth, 36));

        DrawBarcode(g, ink, new Rectangle(16, 194, Width - 32, 40));
        g.DrawString(string.Join(" ", CardNumber(_user).ToCharArray()), Theme.Font(11), ink, 16, 238);
    }

    private static string Initial(string name) => name.Length > 0 ? name[..1].ToUpperInvariant() : "";

    // Decorative bars derived from the card number, so each card always looks the same.
    private void DrawBarcode(Graphics g, Brush ink, Rectangle area)
    {
        var seed = 0;
        foreach (var ch in CardNumber(_user))
            seed = (seed * 31 + ch) % 9973;

        var x = area.X;
        for (var i = 0; x < area.Right; i++)
        {
            seed = (seed * 73 + 41 + i) % 9973;
            var width = 1 + seed % 4;
            var gap = 1 + (seed >> 3) % 3;
            g.FillRectangle(ink, x, area.Y, Math.Min(width, area.Right - x), area.Height);
            x += width + gap;
        }
    }
}
