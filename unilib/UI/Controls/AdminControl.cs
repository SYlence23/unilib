using System.Drawing;
using MaterialSkin.Controls;
using unilib.Models;
using unilib.Services;
using unilib.UI.Forms;

namespace unilib.UI.Controls;

// 04 — Admin Control: library figures, the reservation queue, and adding books.
public class AdminControl : UserControl
{
    private List<Reservation> _reservations = new();
    private bool _pendingOnly = true;

    private readonly TableLayoutPanel _kpis;
    private readonly Panel _queueHost;

    public AdminControl()
    {
        BackColor = Theme.Ground;

        var header = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Theme.Ground };
        header.Controls.Add(Ui.PageHeader("04 — Admin", "Admin Control"));
        var btnAdd = Ui.Button("Add New Book", MaterialButton.MaterialButtonType.Contained, Glyph.Plus);
        btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAdd.Click += BtnAdd_Click;
        header.Controls.Add(btnAdd);
        header.Resize += (s, e) => btnAdd.Location = new Point(header.ClientSize.Width - btnAdd.Width, 32);

        var kpiPanel = new Panel { Dock = DockStyle.Top, Height = 124, BackColor = Theme.Ground };
        _kpis = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Theme.Ground };
        for (var i = 0; i < 4; i++)
            _kpis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        kpiPanel.Controls.Add(_kpis);
        kpiPanel.Controls.Add(Ui.Rule(DockStyle.Top));
        kpiPanel.Controls.Add(Ui.Rule(DockStyle.Bottom));

        var queueHeader = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Theme.Ground };
        var queueTitle = Ui.Text("Reservation Queue", 20, FontStyle.Bold);
        queueTitle.Location = new Point(0, 28);
        queueHeader.Controls.Add(queueTitle);
        var filter = new SegmentedButtons(new[] { "Pending", "All" }) { Anchor = AnchorStyles.Top | AnchorStyles.Right };
        filter.SelectedIndexChanged += (s, e) =>
        {
            _pendingOnly = filter.SelectedIndex == 0;
            RenderQueue();
        };
        queueHeader.Controls.Add(filter);
        queueHeader.Resize += (s, e) => filter.Location = new Point(queueHeader.ClientSize.Width - filter.Width, 24);

        _queueHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Ground };

        // The last control added is docked first, so this stacks header, figures, queue title, queue.
        Controls.Add(_queueHost);
        Controls.Add(queueHeader);
        Controls.Add(kpiPanel);
        Controls.Add(header);

        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            var reservations = new ReservationService().GetAllAsync();
            var loans = new LoanService().GetActiveAsync();
            var books = new BookService().GetAllAsync();
            await Task.WhenAll(reservations, loans, books);
            if (IsDisposed)
                return;

            _reservations = reservations.Result;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var overdue = loans.Result.Count(l => l.DueDate < today);
            RenderKpis(new[]
            {
                ("Total Active Loans", loans.Result.Count, false),
                ("Pending Requests", _reservations.Count(r => r.Status == ReservationStatus.Pending), false),
                ("Overdue Books", overdue, overdue > 0),
                ("Total Books", books.Result.Sum(b => b.Amount ?? 0), false)
            });
            RenderQueue();
        }
        catch (Exception ex)
        {
            if (IsDisposed)
                return;
            ShowInQueue(Ui.EmptyState("Could not load the admin data.", ex.Message));
        }
    }

    private void RenderKpis((string Label, int Value, bool Alert)[] figures)
    {
        _kpis.SuspendLayout();
        while (_kpis.Controls.Count > 0)
            _kpis.Controls[0].Dispose();
        for (var i = 0; i < figures.Length; i++)
            _kpis.Controls.Add(new KpiCell(figures[i].Label, figures[i].Value, figures[i].Alert, last: i == figures.Length - 1), i, 0);
        _kpis.ResumeLayout();
    }

    private void RenderQueue()
    {
        var pending = _reservations.Where(r => r.Status == ReservationStatus.Pending).OrderBy(r => r.ReservationDate).ToList();
        var rows = _pendingOnly
            ? pending
            : pending.Concat(_reservations.Where(r => r.Status != ReservationStatus.Pending)).ToList();

        if (rows.Count == 0)
        {
            ShowInQueue(Ui.EmptyState("The queue is clear."));
            return;
        }

        var list = Ui.Table(("Request ID", 1f), ("Student Name", 1.4f), ("Book Title", 2.2f), ("Requested Date", 1.1f), ("Status", 1f));
        list.MultiSelect = true;
        foreach (var r in rows)
            list.Items.Add(new ListViewItem(new[]
            {
                Ui.RequestId(r.Id), $"{r.User.FirstName} {r.User.LastName}", r.Book.Title,
                r.ReservationDate.ToString("yyyy-MM-dd"), Ui.StatusLabel(r.Status)
            }) { Tag = r });

        var btnApprove = Ui.Button("Approve", MaterialButton.MaterialButtonType.Contained);
        var btnReject = Ui.Button("Reject", MaterialButton.MaterialButtonType.Outlined);
        var btnReopen = Ui.Button("Undo decision", MaterialButton.MaterialButtonType.Text);
        var hint = Ui.Text("Select one or more requests.", 13, FontStyle.Regular, Theme.TextMuted);
        hint.Margin = new Padding(8, 14, 0, 0);

        List<Reservation> Selected() => list.SelectedItems.Cast<ListViewItem>().Select(i => (Reservation)i.Tag!).ToList();
        void UpdateActions()
        {
            var selected = Selected();
            var allPending = selected.Count > 0 && selected.All(r => r.Status == ReservationStatus.Pending);
            btnApprove.Enabled = btnReject.Enabled = allPending;
            btnReopen.Enabled = selected.Count > 0
                && selected.All(r => r.Status is ReservationStatus.Active or ReservationStatus.Cancelled);
        }
        list.SelectedIndexChanged += (s, e) => UpdateActions();

        var service = new ReservationService();
        btnApprove.Click += (s, e) => Decide(Selected(), service.ConfirmAsync, "approved");
        btnReject.Click += (s, e) => Decide(Selected(), service.CancelAsync, "rejected");
        btnReopen.Click += (s, e) => Decide(Selected(), service.ReopenAsync, "moved back to pending");
        UpdateActions();

        var panel = new Panel { BackColor = Theme.Ground };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.Ground, WrapContents = false };
        actions.Controls.AddRange(new Control[] { btnApprove, btnReject, btnReopen, hint });
        panel.Controls.Add(list);
        panel.Controls.Add(actions);
        ShowInQueue(panel);
    }

    private async void Decide(List<Reservation> reservations, Func<int, Task> change, string verb)
    {
        if (reservations.Count == 0)
            return;
        var errors = new List<string>();
        foreach (var reservation in reservations)
        {
            try
            {
                await change(reservation.Id);
            }
            catch (Exception ex)
            {
                errors.Add($"{Ui.RequestId(reservation.Id)}: {ex.Message}");
            }
        }
        if (IsDisposed)
            return;

        var done = reservations.Count - errors.Count;
        var message = done == 1 && errors.Count == 0
            ? $"{Ui.RequestId(reservations[0].Id)} {verb}."
            : $"{done} request{(done == 1 ? "" : "s")} {verb}.";
        Ui.Toast(this, errors.Count == 0 ? message : $"{message} {string.Join(" ", errors)}");
        LoadData();
    }

    private void ShowInQueue(Control control)
    {
        while (_queueHost.Controls.Count > 0)
            _queueHost.Controls[0].Dispose();
        control.Dock = DockStyle.Fill;
        _queueHost.Controls.Add(control);
    }

    private async void BtnAdd_Click(object? sender, EventArgs e)
    {
        List<Category> categories;
        List<Library> libraries;
        try
        {
            var categoryTask = new CategoryService().GetAllAsync();
            var libraryTask = new LibraryService().GetAllAsync();
            await Task.WhenAll(categoryTask, libraryTask);
            (categories, libraries) = (categoryTask.Result, libraryTask.Result);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                Ui.Toast(this, ex.Message);
            return;
        }
        if (IsDisposed)
            return;

        using var dialog = new AddBookForm(categories, libraries);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;
        Ui.Toast(this, $"\"{dialog.AddedTitle}\" added to the catalog.");
        LoadData();
    }

    // One library figure: a small label over a large number, in the accent when it needs attention.
    private class KpiCell : Control
    {
        private readonly string _label;
        private readonly int _value;
        private readonly bool _alert;
        private readonly bool _last;

        public KpiCell(string label, int value, bool alert, bool last)
        {
            _label = label;
            _value = value;
            _alert = alert;
            _last = last;
            Dock = DockStyle.Fill;
            Margin = new Padding(0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.Clear(Theme.Ground);
            using var muted = new SolidBrush(Theme.TextMuted);
            using var value = new SolidBrush(_alert ? Theme.Accent : Theme.Ink);
            g.DrawString(_label.ToUpperInvariant(), Theme.Font(11), muted, 20, 20);
            g.DrawString(_value.ToString("N0"), Theme.Font(48, FontStyle.Bold), value, 14, 42);
            if (!_last)
            {
                using var rule = new Pen(Theme.Divider);
                g.DrawLine(rule, Width - 1, 0, Width - 1, Height);
            }
        }
    }
}
