using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using unilib.Services;

namespace unilib.UI.Controls;

public class ProfileControl : UserControl
{
    private FlowLayoutPanel panelStudentMetadata;
    private Panel panelIdCard;
    private TabControl tabControl;

    public ProfileControl()
    {
        InitializeComponent();
        LoadProfile();
    }

    private void InitializeComponent()
    {
        this.BackColor = Theme.BackgroundMain;
        this.Padding = new Padding(20, 60, 20, 20); // Increased top padding to 60

        var tableLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.BackgroundMain
        };
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        // Left Panel (Metadata & ID Card)
        panelStudentMetadata = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BackgroundMain,
            Padding = new Padding(0, 0, 40, 0), // Added right padding to separate from table
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        var lblProfile = new Label 
        { 
            Text = "Student Profile", 
            Font = Theme.MainFont(16, FontStyle.Bold), 
            ForeColor = Theme.TextPrimary, 
            AutoSize = true, 
            Margin = new Padding(0, 0, 0, 20) 
        };
        panelStudentMetadata.Controls.Add(lblProfile);

        // Digital ID Card
        panelIdCard = new Panel
        {
            Width = 450, // Increased width for better scaling
            Height = 260, // Increased height for better scaling
            BackColor = Theme.Surface,
            Margin = new Padding(0, 0, 0, 20)
        };
        panelIdCard.Paint += PanelIdCard_Paint;
        panelStudentMetadata.Controls.Add(panelIdCard);

        var btnChangePassword = new MaterialSkin.Controls.MaterialButton
        {
            Text = "Change Password",
            Width = 200,
            Height = 40,
            Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained,
            UseAccentColor = false
        };
        panelStudentMetadata.Controls.Add(btnChangePassword);

        tableLayout.Controls.Add(panelStudentMetadata, 0, 0);

        // Right TabControl
        tabControl = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = Theme.MainFont(11)
        };

        var tabBookings = new TabPage("My Bookings") { BackColor = Theme.Surface };
        var tabSaved = new TabPage("Saved Books") { BackColor = Theme.Surface };
        var tabLimits = new TabPage("Borrowing Limits") { BackColor = Theme.Surface };

        // Bookings DataGridView
        var dgvBookings = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Theme.Surface,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Theme.Border,
            RowTemplate = { Height = 40 }
        };
        dgvBookings.ColumnHeadersDefaultCellStyle.BackColor = Theme.Surface;
        dgvBookings.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextMuted;
        dgvBookings.ColumnHeadersDefaultCellStyle.Font = Theme.MainFont(11, FontStyle.Bold);
        dgvBookings.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgvBookings.DefaultCellStyle.BackColor = Theme.Surface;
        dgvBookings.DefaultCellStyle.ForeColor = Theme.TextPrimary;
        dgvBookings.DefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
        dgvBookings.AlternatingRowsDefaultCellStyle.BackColor = Theme.BackgroundMain;
        
        tabBookings.Controls.Add(dgvBookings);

        tabControl.TabPages.Add(tabBookings);
        tabControl.TabPages.Add(tabSaved);
        tabControl.TabPages.Add(tabLimits);

        tableLayout.Controls.Add(tabControl, 1, 0);

        this.Controls.Add(tableLayout);
    }

    private void PanelIdCard_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, panelIdCard.Width, panelIdCard.Height);
        
        // Pearlescent light gradient
        using (var brush = new LinearGradientBrush(rect, Color.White, Theme.SurfaceHover, 45f))
        {
            g.FillRoundedRectangle(brush, rect, 12);
        }

        // Border
        using (var pen = new Pen(Theme.Border, 1))
        {
            g.DrawRoundedRectangle(pen, rect, 12);
        }

        var fontBold = Theme.MainFont(14, FontStyle.Bold);
        var fontReg = Theme.MainFont(11);
        
        var user = Session.CurrentUser;
        if (user != null)
        {
            // Draw Profile Icon Placeholder (gray rounded rect with silhouette)
            int photoX = 20;
            int photoY = 40;
            int photoSize = 80;
            using (var photoBrush = new SolidBrush(ColorTranslator.FromHtml("#E2E8F0")))
            {
                g.FillRoundedRectangle(photoBrush, new Rectangle(photoX, photoY, photoSize, photoSize), 8);
            }
            using (var silBrush = new SolidBrush(ColorTranslator.FromHtml("#94A3B8")))
            {
                g.FillEllipse(silBrush, photoX + 25, photoY + 15, 30, 30); // Head
                g.FillEllipse(silBrush, photoX + 10, photoY + 50, 60, 40); // Body
            }

            // Draw Texts
            int textX = 120;
            g.DrawString("UniLib Student ID", fontBold, new SolidBrush(Theme.Primary), textX, 20);
            
            // Draw simulated barcode
            using (var barcodePen = new Pen(Color.Black, 2))
            using (var barcodePenThick = new Pen(Color.Black, 4))
            {
                int bcX = textX;
                int bcY = 50;
                for (int i = 0; i < 40; i++)
                {
                    Pen p = (i % 3 == 0 || i % 7 == 0) ? barcodePenThick : barcodePen;
                    if (i % 5 != 0) g.DrawLine(p, bcX + (i * 4), bcY, bcX + (i * 4), bcY + 30);
                }
            }

            g.DrawString($"{user.FirstName} {user.LastName}", fontBold, new SolidBrush(Theme.TextPrimary), textX, 90);
            g.DrawString($"ID: {user.StudentId ?? "V123"}", fontReg, new SolidBrush(Theme.TextMuted), textX, 115);
            g.DrawString($"{user.Email}", fontReg, new SolidBrush(Theme.TextMuted), textX, 135);
        }
        

    }

    private async void LoadProfile()
    {
        if (Session.CurrentUser == null) return;
        panelIdCard.Invalidate();

        try
        {
            var resService = new ReservationService();
            var reservations = await resService.GetByUserAsync(Session.CurrentUser.Id);

            var dgv = tabControl.TabPages[0].Controls[0] as DataGridView;
            if (dgv != null)
            {
                dgv.DataSource = reservations;
            }
        }
        catch { }
    }
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
    {
        using (GraphicsPath path = GetRoundedRectanglePath(rect, radius))
        {
            g.FillPath(brush, path);
        }
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
    {
        using (GraphicsPath path = GetRoundedRectanglePath(rect, radius))
        {
            g.DrawPath(pen, path);
        }
    }

    private static GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
    {
        int diameter = radius * 2;
        Size size = new Size(diameter, diameter);
        Rectangle arc = new Rectangle(rect.Location, size);
        GraphicsPath path = new GraphicsPath();

        if (radius == 0)
        {
            path.AddRectangle(rect);
            return path;
        }

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
