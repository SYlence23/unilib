using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace unilib.UI.Controls;

public class MapControl : UserControl
{
    private TreeView tvFacultyLocations;
    private Panel picMapCanvas;

    public MapControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.BackColor = Theme.BackgroundMain;
        this.Padding = new Padding(20, 40, 20, 20);

        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 300,
            BackColor = Theme.Border
        };

        // Left Panel (TreeView)
        tvFacultyLocations = new TreeView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextPrimary,
            Font = Theme.MainFont(11),
            ItemHeight = 30
        };
        
        var nodeScience = tvFacultyLocations.Nodes.Add("Science Faculty Library");
        nodeScience.Nodes.Add("Floor 1 - Biology");
        nodeScience.Nodes.Add("Floor 2 - Physics");
        
        var nodeArts = tvFacultyLocations.Nodes.Add("Arts Faculty Library");
        nodeArts.Nodes.Add("Floor 1 - History");
        nodeArts.Nodes.Add("Floor 2 - Literature");

        tvFacultyLocations.ExpandAll();

        splitContainer.Panel1.Controls.Add(tvFacultyLocations);

        // Right Panel (Map Canvas)
        picMapCanvas = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.BackgroundMain // light warm canvas
        };
        picMapCanvas.Paint += PicMapCanvas_Paint;

        splitContainer.Panel2.Controls.Add(picMapCanvas);

        this.Controls.Add(splitContainer);
    }

    private void PicMapCanvas_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Calculate scaling
        float w = picMapCanvas.Width;
        float h = picMapCanvas.Height;
        if (w < 100 || h < 100) return; // safeguard

        // Base coordinate system was 600x400. Let's scale it to fit 80% of canvas
        float scaleX = (w * 0.8f) / 600f;
        float scaleY = (h * 0.8f) / 400f;
        float scale = Math.Min(scaleX, scaleY); // Keep aspect ratio
        
        float offsetX = (w - (600f * scale)) / 2f;
        float offsetY = (h - (400f * scale)) / 2f;

        g.TranslateTransform(offsetX, offsetY);
        g.ScaleTransform(scale, scale);

        // Draw blueprint layout
        using (var penWall = new Pen(Theme.TextPrimary, 3 / scale))
        {
            g.DrawRectangle(penWall, 50, 50, 600, 400); // Main room
            g.DrawLine(penWall, 50, 200, 300, 200);     // Inner wall
        }

        using (var penAisle = new Pen(Theme.Border, 2 / scale))
        {
            for (int i = 0; i < 5; i++)
            {
                g.DrawRectangle(penAisle, 100 + (i * 60), 100, 20, 80); // Bookshelves
            }
        }

        // Draw glowing dashed navigation path
        using (var penPath = new Pen(Theme.Primary, 3 / scale))
        {
            penPath.DashStyle = DashStyle.Dash;
            g.DrawLine(penPath, 50, 400, 250, 400);
            g.DrawLine(penPath, 250, 400, 250, 250);
            g.DrawLine(penPath, 250, 250, 360, 250);
            g.DrawLine(penPath, 360, 250, 360, 150);
        }

        // Target marker
        using (var brushMarker = new SolidBrush(Theme.Primary))
        {
            g.FillEllipse(brushMarker, 350, 140, 20, 20);
        }
    }
}
