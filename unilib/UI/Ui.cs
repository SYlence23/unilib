using System.Drawing;
using System.Runtime.InteropServices;
using MaterialSkin.Controls;
using unilib.Models;

namespace unilib.UI;

// Builders for the pieces every screen repeats, so they all look the same.
public static class Ui
{
    public static Label Text(string text, float px, FontStyle style = FontStyle.Regular, Color? color = null) => new()
    {
        Text = text,
        Font = Theme.Font(px, style),
        ForeColor = color ?? Theme.TextPrimary,
        BackColor = Color.Transparent,
        AutoSize = true,
        Margin = new Padding(0)
    };

    // The small uppercase line above a heading ("01 — CATALOG").
    public static Label Overline(string text) => Text(text.ToUpperInvariant(), 11, FontStyle.Regular, Theme.TextMuted);

    // Overline + page title, stacked.
    public static Control PageHeader(string overline, string title)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        panel.Controls.Add(Overline(overline));
        var heading = Text(title, 32, FontStyle.Bold);
        heading.Margin = new Padding(0, 6, 0, 0);
        panel.Controls.Add(heading);
        return panel;
    }

    public static MaterialButton Button(string text, MaterialButton.MaterialButtonType type,
        Glyph? glyph = null, bool dense = false)
    {
        var button = new MaterialButton
        {
            Text = text,
            Type = type,
            HighEmphasis = true,
            UseAccentColor = false,
            CharacterCasing = MaterialButton.CharacterCasingEnum.Normal,
            Density = dense ? MaterialButton.MaterialButtonDensity.Dense : MaterialButton.MaterialButtonDensity.Default,
            AutoSize = true,
            Margin = new Padding(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        if (glyph != null)
            button.Icon = Icons.Bitmap(glyph.Value, Color.Black);
        return button;
    }

    public static MaterialTextBox2 TextField(string hint, int width, bool password = false) => new()
    {
        Hint = hint,
        Width = width,
        UseSystemPasswordChar = password,
        Margin = new Padding(0, 0, 0, 12)
    };

    public static Label ErrorLabel(int width) => new()
    {
        ForeColor = Theme.Accent700,
        BackColor = Color.Transparent,
        Font = Theme.Font(13),
        AutoSize = false,
        Size = new Size(width, 20),
        Margin = new Padding(0, 0, 0, 8),
        Visible = false
    };

    public static void ShowError(Label label, string message)
    {
        label.Text = message;
        label.Visible = true;
    }

    // A strong 2px rule, the design's section divider.
    public static Panel Rule(DockStyle dock = DockStyle.Top, int thickness = 2) => new()
    {
        Dock = dock,
        Height = thickness,
        Width = thickness,
        BackColor = Theme.Divider
    };

    // Material list with columns that share the width by weight.
    public static MaterialListView Table(params (string Header, float Weight)[] columns)
    {
        var list = new MaterialListView
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
            AutoSizeTable = false,
            BorderStyle = BorderStyle.None
        };
        foreach (var column in columns)
            list.Columns.Add(column.Header);

        void Fit()
        {
            var total = columns.Sum(c => c.Weight);
            var width = list.ClientSize.Width - 4;
            for (var i = 0; i < columns.Length; i++)
                list.Columns[i].Width = (int)(width * columns[i].Weight / total);
        }
        list.Resize += (s, e) => Fit();
        return list;
    }

    // Placeholder shown instead of an empty list.
    public static Control EmptyState(string title, string? text = null, string? buttonText = null, Action? onClick = null)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 32, 0, 0)
        };
        panel.Controls.Add(Text(title, 18, FontStyle.Bold));
        if (text != null)
        {
            var body = Text(text, 14, FontStyle.Regular, Theme.TextMuted);
            body.Margin = new Padding(0, 8, 0, 0);
            panel.Controls.Add(body);
        }
        if (buttonText != null && onClick != null)
        {
            var button = Button(buttonText, MaterialButton.MaterialButtonType.Outlined);
            button.Margin = new Padding(0, 12, 0, 0);
            // Deferred: the action usually replaces this panel, disposing the button mid-click.
            button.Click += (s, e) => button.BeginInvoke(onClick);
            panel.Controls.Add(button);
        }
        return panel;
    }

    // Short message at the bottom of the window (Material snackbar).
    public static void Toast(Control anchor, string message)
    {
        var form = anchor.FindForm();
        if (form == null || form.IsDisposed)
            return;
        new MaterialSnackBar(message, 2800).Show(form);
    }

    public static string RequestId(int reservationId) => $"REQ-{reservationId:D4}";

    public static string StatusLabel(string status) => status switch
    {
        ReservationStatus.Pending => "Pending",
        ReservationStatus.Active => "Approved",
        ReservationStatus.Cancelled => "Cancelled",
        ReservationStatus.Completed => "Completed",
        _ => status
    };

    public static string RoleLabel(User user) => user.Role?.Name switch
    {
        RoleNames.Admin => "Admin",
        RoleNames.Librarian => "Librarian",
        _ => "Student"
    };

    public static bool IsStaff(User? user) =>
        user?.Role?.Name is RoleNames.Admin or RoleNames.Librarian;

    // Lets a borderless MaterialForm be dragged by one of its own panels.
    public static void DragsWindow(Control handle)
    {
        handle.MouseDown += (s, e) =>
        {
            if (e.Button != MouseButtons.Left || handle.FindForm() is not Form form)
                return;
            ReleaseCapture();
            SendMessage(form.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        };
    }

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
}
