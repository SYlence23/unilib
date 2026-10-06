using System;
using System.Drawing;
using System.Windows.Forms;

namespace unilib.UI.Controls;

public class AdminControl : UserControl
{
    private TableLayoutPanel tlpKpis;
    private DataGridView dgvQueue;

    public AdminControl()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.BackColor = Theme.BackgroundMain;
        this.Padding = new Padding(20, 40, 20, 20);

        // KPI Panel
        tlpKpis = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 120,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.BackgroundMain
        };
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        tlpKpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        tlpKpis.Controls.Add(CreateKpiCard("Total Active Loans", "1,245", Theme.Primary), 0, 0);
        tlpKpis.Controls.Add(CreateKpiCard("Pending Requests", "18", Theme.Warning), 1, 0);
        tlpKpis.Controls.Add(CreateKpiCard("Overdue Books", "7", Theme.Danger), 2, 0);
        tlpKpis.Controls.Add(CreateKpiCard("Total Books", "42,010", Theme.Success), 3, 0);

        this.Controls.Add(tlpKpis);

        var marginPanel = new Panel { Height = 20, Dock = DockStyle.Top, BackColor = Theme.BackgroundMain };
        this.Controls.Add(marginPanel);

        var panelHeader = new Panel { Height = 50, Dock = DockStyle.Top, BackColor = Theme.BackgroundMain };
        
        var lblQueue = new Label { Text = "Reservation Queue", Font = Theme.MainFont(14, FontStyle.Bold), ForeColor = Theme.TextPrimary, AutoSize = true, Dock = DockStyle.Left };
        panelHeader.Controls.Add(lblQueue);

        var btnAddBook = new MaterialSkin.Controls.MaterialButton
        {
            Text = "+ Add New Book",
            Width = 150,
            Height = 35,
            Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained,
            UseAccentColor = false,
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand
        };
        btnAddBook.Click += (s, e) => MessageBox.Show("Add Book Modal not implemented.", "Info");
        panelHeader.Controls.Add(btnAddBook);

        this.Controls.Add(panelHeader);

        // DataGridView for Queue
        dgvQueue = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Theme.Surface,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            AllowUserToAddRows = false
        };
        dgvQueue.ColumnHeadersDefaultCellStyle.BackColor = Theme.SurfaceHover;
        dgvQueue.ColumnHeadersDefaultCellStyle.ForeColor = Theme.TextPrimary;
        dgvQueue.DefaultCellStyle.BackColor = Theme.Surface;
        dgvQueue.DefaultCellStyle.ForeColor = Theme.TextPrimary;
        dgvQueue.AlternatingRowsDefaultCellStyle.BackColor = Theme.BackgroundMain;
        
        dgvQueue.Columns.Add("Id", "Request ID");
        dgvQueue.Columns.Add("Student", "Student Name");
        dgvQueue.Columns.Add("Book", "Book Title");
        dgvQueue.Columns.Add("Date", "Requested Date");

        var btnApprove = new DataGridViewButtonColumn { HeaderText = "Action", Text = "Approve", UseColumnTextForButtonValue = true };
        dgvQueue.Columns.Add(btnApprove);

        var btnReject = new DataGridViewButtonColumn { HeaderText = "Action", Text = "Reject", UseColumnTextForButtonValue = true };
        dgvQueue.Columns.Add(btnReject);

        // Add dummy row for visual
        dgvQueue.Rows.Add("REQ-1001", "John Doe", "The Great Gatsby", "2026-10-03");

        this.Controls.Add(dgvQueue);
    }

    private Control CreateKpiCard(string title, string value, Color color)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(10),
            BackColor = Theme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblTitle = new Label { Text = title, Font = Theme.MainFont(10), ForeColor = Theme.TextMuted, AutoSize = true, Location = new Point(15, 15) };
        var lblValue = new Label { Text = value, Font = Theme.MainFont(24, FontStyle.Bold), ForeColor = Theme.TextPrimary, AutoSize = true, Location = new Point(15, 40) };
        
        card.Controls.Add(lblTitle);
        card.Controls.Add(lblValue);

        return card;
    }
}
