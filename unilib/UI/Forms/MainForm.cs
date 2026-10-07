using System;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using unilib.Services;
using unilib.UI.Controls;

namespace unilib.UI.Forms;

public class MainForm : Form
{
    private FlowLayoutPanel panelSidebar;
    private Panel panelContentContainer;
    private StatusStrip statusBar;
    private ToolStripStatusLabel statusUser;

    private Button btnCatalog;
    private Button btnProfile;
    private Button btnMap;
    private Button btnAdmin;

    public MainForm()
    {
        InitializeComponent();
        
        // Removed MaterialSkinManager from MainForm to avoid conflicts


        LoadUserContext();
        // Load default tab
        btnCatalog.PerformClick();
    }

    private void InitializeComponent()
    {
        this.Text = "UniLib - Desktop";
        this.Size = new Size(1440, 1024);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Theme.BackgroundMain;

        // Status Bar
        statusBar = new StatusStrip();
        statusBar.BackColor = Theme.SurfaceHover;
        statusBar.ForeColor = Theme.TextMuted;
        statusUser = new ToolStripStatusLabel();
        statusBar.Items.Add(statusUser);
        this.Controls.Add(statusBar);

        // Header Panel
        var panelHeader = new Panel
        {
            Height = 60,
            Dock = DockStyle.Top,
            BackColor = Theme.Primary
        };

        btnToggleMenu = new Button
        {
            Text = "☰",
            Font = Theme.MainFont(18, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Theme.Primary,
            Width = 60,
            Dock = DockStyle.Left,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnToggleMenu.FlatAppearance.BorderSize = 0;
        btnToggleMenu.Click += BtnToggleMenu_Click;
        panelHeader.Controls.Add(btnToggleMenu);

        lblLogo = new Label
        {
            Text = "UniLib",
            Font = Theme.MainFont(20, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(70, 15)
        };
        panelHeader.Controls.Add(lblLogo);

        this.Controls.Add(panelHeader);

        // Sidebar
        panelSidebar = new FlowLayoutPanel
        {
            Width = 260,
            Dock = DockStyle.Left,
            BackColor = Theme.Surface,
            Padding = new Padding(10, 20, 10, 10),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        // Navigation Buttons
        btnCatalog = CreateNavButton("Catalog");
        btnProfile = CreateNavButton("Student Profile");
        btnMap = CreateNavButton("Campus Map");
        btnAdmin = CreateNavButton("Admin Control");

        panelSidebar.Controls.Add(btnCatalog);
        panelSidebar.Controls.Add(btnProfile);
        panelSidebar.Controls.Add(btnMap);
        panelSidebar.Controls.Add(btnAdmin);

        this.Controls.Add(panelSidebar);
        panelSidebar.BringToFront(); // Dock Left above Fill
        panelHeader.BringToFront();  // Dock Top above Left

        // Content Container
        panelContentContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BackgroundMain,
            Padding = new Padding(20)
        };
        this.Controls.Add(panelContentContainer);
        panelContentContainer.SendToBack(); // Fill goes behind everything
    }

    private Button btnToggleMenu;
    private Label lblLogo;
    private bool isMenuExpanded = true;

    private void BtnToggleMenu_Click(object? sender, EventArgs e)
    {
        isMenuExpanded = !isMenuExpanded;
        panelSidebar.Visible = isMenuExpanded;
    }

    private Button CreateNavButton(string text)
    {
        var btn = new Button
        {
            Text = "  " + text,
            Width = 240,
            Height = 50,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.MainFont(12, FontStyle.Regular),
            ForeColor = Theme.TextPrimary,
            BackColor = Theme.Surface,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 10)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += NavButton_Click;
        return btn;
    }

    private void LoadUserContext()
    {
        if (Session.CurrentUser != null)
        {
            statusUser.Text = $"Logged in as: {Session.CurrentUser.FirstName} {Session.CurrentUser.LastName}";
            // Hide admin button if not admin
            if (!Session.IsInRole("admin") && !Session.IsInRole("librarian"))
            {
                btnAdmin.Visible = false;
            }
        }
    }

    private void NavButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button clickedBtn) return;

        // Reset all buttons style
        foreach (Control ctrl in panelSidebar.Controls)
        {
            if (ctrl is Button b)
            {
                b.BackColor = Theme.Surface;
                b.ForeColor = Theme.TextPrimary;
                b.Font = Theme.MainFont(12, FontStyle.Regular);
            }
        }

        // Set active button style
        clickedBtn.BackColor = Theme.Primary; // Deep Purple
        clickedBtn.ForeColor = Color.White;
        clickedBtn.Font = Theme.MainFont(12, FontStyle.Bold);

        // Switch UserControl based on button
        panelContentContainer.Controls.Clear();
        UserControl? uc = null;

        if (clickedBtn == btnCatalog) uc = new CatalogControl();
        else if (clickedBtn == btnProfile) uc = new ProfileControl();
        else if (clickedBtn == btnMap) uc = new MapControl();
        else if (clickedBtn == btnAdmin) uc = new AdminControl();

        if (uc != null)
        {
            uc.Dock = DockStyle.Fill;
            panelContentContainer.Controls.Add(uc);
        }
    }
}
