using System;
using System.Drawing;
using System.Windows.Forms;
using unilib.Services;
using unilib.Models;

namespace unilib.UI.Controls;

public class CatalogControl : UserControl
{
    private FlowLayoutPanel panelFilters;
    private FlowLayoutPanel panelGrid;
    private MaterialSkin.Controls.MaterialTextBox txtSearch;
    private ComboBox cmbCategory;

    public CatalogControl()
    {
        InitializeComponent();
        LoadBooks();
    }

    private void InitializeComponent()
    {
        this.BackColor = Theme.BackgroundMain;
        this.Padding = new Padding(20, 40, 20, 20); // Increased top padding

        // Filters Panel
        panelFilters = new FlowLayoutPanel
        {
            Height = 80,
            Dock = DockStyle.Top,
            BackColor = Theme.Surface,
            Padding = new Padding(15, 20, 15, 15),
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight
        };

        var lblTitle = new Label 
        { 
            Text = "Catalog & Discovery", 
            Font = Theme.MainFont(18, FontStyle.Bold), 
            ForeColor = Theme.TextPrimary, 
            AutoSize = true, 
            Margin = new Padding(0, 5, 30, 0) 
        };
        panelFilters.Controls.Add(lblTitle);

        cmbCategory = new ComboBox
        {
            Width = 150,
            Font = Theme.MainFont(12),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 5, 15, 0)
        };
        cmbCategory.Items.AddRange(new object[] { "All Categories", "Sci-Fi", "Fiction", "Academic", "Technology" });
        cmbCategory.SelectedIndex = 0;
        panelFilters.Controls.Add(cmbCategory);

        txtSearch = new MaterialSkin.Controls.MaterialTextBox
        { 
            Width = 300, 
            Font = Theme.MainFont(12), 
            Hint = "Search books...", 
            Margin = new Padding(0, 5, 15, 0)
        };
        panelFilters.Controls.Add(txtSearch);

        var btnSearch = new MaterialSkin.Controls.MaterialButton
        { 
            Text = "Search", 
            Width = 120, 
            Height = 35, 
            Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained,
            UseAccentColor = false,
            Margin = new Padding(0, 3, 0, 0)
        };
        btnSearch.Click += BtnSearch_Click;
        panelFilters.Controls.Add(btnSearch);

        this.Controls.Add(panelFilters);

        // Margin panel
        var marginPanel = new Panel { Height = 20, Dock = DockStyle.Top, BackColor = Theme.BackgroundMain };
        this.Controls.Add(marginPanel);

        // Grid Panel
        panelGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.BackgroundMain
        };
        this.Controls.Add(panelGrid);
    }

    private async void LoadBooks()
    {
        try
        {
            var bookService = new BookService();
            var books = await bookService.SearchAsync(txtSearch.Text);
            
            panelGrid.Controls.Clear();
            foreach (var book in books)
            {
                panelGrid.Controls.Add(CreateBookCard(book));
            }
            if (books.Count == 0)
            {
                panelGrid.Controls.Add(new Label { Text = "No books found.", AutoSize = true, Font = Theme.MainFont(12), ForeColor = Theme.TextMuted, Margin = new Padding(10) });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error loading books");
        }
    }

    private Control CreateBookCard(Book book)
    {
        var card = new Panel
        {
            Width = 220,
            Height = 330,
            BackColor = Theme.Surface,
            Margin = new Padding(12),
            BorderStyle = BorderStyle.FixedSingle // Fake border since we don't paint shadows
        };

        var lblTitle = new Label
        {
            Text = book.Title,
            Font = Theme.MainFont(12, FontStyle.Bold),
            ForeColor = Theme.TextPrimary,
            Location = new Point(10, 160),
            Width = 200,
            Height = 40
        };
        card.Controls.Add(lblTitle);

        var lblIsbn = new Label
        {
            Text = "ISBN: " + book.Isbn,
            Font = Theme.MainFont(9),
            ForeColor = Theme.TextMuted,
            Location = new Point(10, 210),
            AutoSize = true
        };
        card.Controls.Add(lblIsbn);

        var btnReserve = new Button
        {
            Text = "Reserve",
            Location = new Point(10, 280),
            Width = 200,
            Height = 35,
            BackColor = Theme.SurfaceHover,
            ForeColor = Theme.Primary,
            FlatStyle = FlatStyle.Flat
        };
        btnReserve.FlatAppearance.BorderColor = Theme.Border;
        btnReserve.Click += async (s, e) => {
            if (Session.CurrentUser == null) return;
            try
            {
                var resService = new ReservationService();
                await resService.CreateAsync(Session.CurrentUser.Id, book.Isbn);
                MessageBox.Show("Book reserved!", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        };
        card.Controls.Add(btnReserve);

        return card;
    }

    private void BtnSearch_Click(object? sender, EventArgs e)
    {
        LoadBooks();
    }
}
