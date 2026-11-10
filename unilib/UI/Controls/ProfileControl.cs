using System.Drawing;
using MaterialSkin.Controls;
using unilib.Models;
using unilib.Services;
using unilib.UI.Forms;

namespace unilib.UI.Controls;

// 02 — Student Profile: the library card, and tabs for bookings, saved books and borrowing limits.
public class ProfileControl : UserControl
{
    private readonly User _user;
    private CatalogData _data = new();

    private readonly MaterialTabSelector _tabSelector;
    private readonly TabPage _bookingsPage;
    private readonly TabPage _savedPage;
    private readonly TabPage _limitsPage;

    public ProfileControl()
    {
        _user = Session.CurrentUser!;
        BackColor = Theme.Ground;

        var left = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 490,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Ground
        };
        var header = Ui.PageHeader("02 — Profile", "Student Profile");
        header.Margin = new Padding(0, 0, 0, 20);
        left.Controls.Add(header);
        left.Controls.Add(new IdCardPanel(_user) { Margin = new Padding(0, 0, 0, 20) });
        var btnPassword = Ui.Button("Change Password", MaterialButton.MaterialButtonType.Outlined);
        btnPassword.Click += BtnPassword_Click;
        left.Controls.Add(btnPassword);

        var right = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Ground, Padding = new Padding(0, 48, 0, 0) };
        var tabs = new MaterialTabControl { Dock = DockStyle.Fill };
        _bookingsPage = NewPage("My Bookings");
        _savedPage = NewPage("Saved Books");
        _limitsPage = NewPage("Borrowing Limits");
        tabs.TabPages.AddRange(new[] { _bookingsPage, _savedPage, _limitsPage });
        _tabSelector = new MaterialTabSelector
        {
            Dock = DockStyle.Top,
            Height = 48,
            BaseTabControl = tabs,
            CharacterCasing = MaterialTabSelector.CustomCharacterCasing.Normal
        };
        right.Controls.Add(tabs);
        right.Controls.Add(_tabSelector);

        Controls.Add(right);
        Controls.Add(left);

        LoadData();
    }

    public event EventHandler? BrowseCatalogRequested;
    public event EventHandler<Book>? LocateRequested;

    private static TabPage NewPage(string text) => new(text) { BackColor = Theme.Ground, Padding = new Padding(0, 12, 0, 0) };

    private async void LoadData()
    {
        try
        {
            _data = await CatalogData.LoadAsync(_user.Id);
            if (IsDisposed)
                return;
            Render();
        }
        catch (Exception ex)
        {
            if (IsDisposed)
                return;
            Fill(_bookingsPage, Ui.EmptyState("Could not load your bookings.", ex.Message));
        }
    }

    private void Render()
    {
        var saved = _data.Books.Where(b => _data.IsSaved(b.Isbn)).ToList();
        _bookingsPage.Text = $"My Bookings  {_data.MyReservations.Count}";
        _savedPage.Text = $"Saved Books  {saved.Count}";
        _tabSelector.Invalidate();

        Fill(_bookingsPage, _data.MyReservations.Count == 0
            ? Ui.EmptyState("No bookings yet.", null, "Browse the catalog", () => BrowseCatalogRequested?.Invoke(this, EventArgs.Empty))
            : BuildBookings());
        Fill(_savedPage, saved.Count == 0
            ? Ui.EmptyState("No saved books.", "Use the bookmark on any catalog card to keep it here.")
            : BuildSaved(saved));
        Fill(_limitsPage, BuildLimits());
    }

    private static void Fill(TabPage page, Control content)
    {
        while (page.Controls.Count > 0)
            page.Controls[0].Dispose();
        content.Dock = DockStyle.Fill;
        page.Controls.Add(content);
    }

    private Control BuildBookings()
    {
        var list = Ui.Table(("Request", 1f), ("Book Title", 2.6f), ("Reserved", 1.1f), ("Due", 1.1f), ("Status", 1f));
        foreach (var reservation in _data.MyReservations)
        {
            var due = reservation.Status == ReservationStatus.Active
                ? _data.MyActiveLoans.FirstOrDefault(l => l.BookIsbn == reservation.BookId)?.DueDate.ToString("yyyy-MM-dd")
                : null;
            list.Items.Add(new ListViewItem(new[]
            {
                Ui.RequestId(reservation.Id), reservation.Book.Title,
                reservation.ReservationDate.ToString("yyyy-MM-dd"), due ?? "—", Ui.StatusLabel(reservation.Status)
            }) { Tag = reservation });
        }

        var btnCancel = Ui.Button("Cancel reservation", MaterialButton.MaterialButtonType.Outlined);
        var hint = Ui.Text("Pending and approved reservations can be cancelled.", 13, FontStyle.Regular, Theme.TextMuted);
        Reservation? Selected() => list.SelectedItems.Count > 0 ? (Reservation)list.SelectedItems[0].Tag! : null;
        void UpdateActions() => btnCancel.Enabled =
            Selected()?.Status is ReservationStatus.Pending or ReservationStatus.Active;

        list.SelectedIndexChanged += (s, e) => UpdateActions();
        btnCancel.Click += async (s, e) =>
        {
            if (Selected() is not { } reservation || !Confirm(reservation))
                return;
            try
            {
                await new ReservationService().CancelAsync(reservation.Id);
                if (IsDisposed)
                    return;
                Ui.Toast(this, $"{Ui.RequestId(reservation.Id)} cancelled.");
                LoadData();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    Ui.Toast(this, ex.Message);
            }
        };
        UpdateActions();
        return WithActions(list, btnCancel, hint);
    }

    private bool Confirm(Reservation reservation)
    {
        var form = FindForm()!;
        using var dialog = new MaterialDialog(form, "Cancel reservation",
            $"Cancel {Ui.RequestId(reservation.Id)} for \"{reservation.Book.Title}\"?",
            "Cancel reservation", true, "Keep it");
        return dialog.ShowDialog(form) == DialogResult.OK;
    }

    private Control BuildSaved(List<Book> saved)
    {
        var list = Ui.Table(("Title", 2.4f), ("Author", 1.6f), ("Location", 1.8f), ("Availability", 1.1f), ("Status", 0.9f));
        foreach (var book in saved)
            list.Items.Add(new ListViewItem(new[]
            {
                book.Title, CatalogData.Authors(book), _data.Location(book), _data.AvailabilityLabel(book),
                _data.IsHeld(book.Isbn) ? "Reserved" : ""
            }) { Tag = book });

        var btnReserve = Ui.Button("Reserve", MaterialButton.MaterialButtonType.Contained);
        var btnLocate = Ui.Button("Locate", MaterialButton.MaterialButtonType.Outlined, Glyph.Pin);
        var btnRemove = Ui.Button("Remove", MaterialButton.MaterialButtonType.Text);
        Book? Selected() => list.SelectedItems.Count > 0 ? (Book)list.SelectedItems[0].Tag! : null;
        void UpdateActions()
        {
            var book = Selected();
            btnReserve.Enabled = book != null && !_data.IsHeld(book.Isbn);
            btnLocate.Enabled = book?.LibraryId != null;
            btnRemove.Enabled = book != null;
        }

        list.SelectedIndexChanged += (s, e) => UpdateActions();
        btnLocate.Click += (s, e) => { if (Selected() is { } book) LocateRequested?.Invoke(this, book); };
        btnReserve.Click += async (s, e) =>
        {
            if (Selected() is not { } book)
                return;
            try
            {
                var message = await _data.ReserveAsync(_user.Id, book);
                if (IsDisposed)
                    return;
                Ui.Toast(this, message);
                Render();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    Ui.Toast(this, ex.Message);
            }
        };
        btnRemove.Click += async (s, e) =>
        {
            if (Selected() is not { } book)
                return;
            try
            {
                await _data.ToggleSavedAsync(_user.Id, book);
                if (!IsDisposed)
                    Render();
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    Ui.Toast(this, ex.Message);
            }
        };
        UpdateActions();
        return WithActions(list, btnReserve, btnLocate, btnRemove);
    }

    private Control BuildLimits()
    {
        var grid = new TableLayoutPanel { ColumnCount = 2, RowCount = 2, BackColor = Theme.Ground, Padding = new Padding(0, 8, 0, 0) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));

        grid.Controls.Add(new LimitCell("Active loans", _data.MyActiveLoans.Count, LibraryRules.MaxActiveLoans), 0, 0);
        grid.Controls.Add(new LimitCell("Open reservations", _data.OpenReservations, LibraryRules.MaxOpenReservations), 1, 0);
        grid.Controls.Add(new LimitCell("Loan period", LibraryRules.LoanPeriodDays, suffix: "days"), 0, 1);
        grid.Controls.Add(new LimitCell("Renewals per loan", LibraryRules.RenewalsPerLoan), 1, 1);
        return grid;
    }

    // A list with a row of action buttons above it.
    private static Control WithActions(Control list, params Control[] actions)
    {
        var panel = new Panel { BackColor = Theme.Ground };
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.Ground, WrapContents = false };
        foreach (var action in actions)
        {
            if (action is Label)
                action.Margin = new Padding(8, 14, 0, 0);
            bar.Controls.Add(action);
        }
        panel.Controls.Add(list);
        panel.Controls.Add(bar);
        return panel;
    }

    private void BtnPassword_Click(object? sender, EventArgs e)
    {
        using var dialog = new ChangePasswordForm();
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            Ui.Toast(this, "Password updated.");
    }

    // One figure on the Borrowing Limits tab, with a usage bar when there is a limit.
    private class LimitCell : Control
    {
        private readonly string _label;
        private readonly int _value;
        private readonly int? _limit;
        private readonly string? _suffix;

        public LimitCell(string label, int value, int? limit = null, string? suffix = null)
        {
            _label = label;
            _value = value;
            _limit = limit;
            _suffix = suffix;
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
            using var ink = new SolidBrush(Theme.Ink);
            using var muted = new SolidBrush(Theme.TextMuted);
            using var rule = new Pen(Theme.Neutral300);
            g.DrawRectangle(rule, 0, 0, Width - 1, Height - 1);

            g.DrawString(_label.ToUpperInvariant(), Theme.Font(11), muted, 16, 20);
            var big = Theme.Font(40, FontStyle.Bold);
            var value = _value.ToString();
            g.DrawString(value, big, ink, 12, 42);
            var after = _limit != null ? $"/ {_limit}" : _suffix;
            if (after != null)
                g.DrawString(after, Theme.Font(20, FontStyle.Bold), muted, 16 + g.MeasureString(value, big).Width, 60);

            if (_limit is { } limit)
            {
                using var track = new SolidBrush(Theme.Neutral300);
                using var bar = new SolidBrush(Theme.Accent);
                var width = Width - 32;
                g.FillRectangle(track, 16, 106, width, 6);
                g.FillRectangle(bar, 16, 106, width * Math.Min(_value, limit) / limit, 6);
            }
        }
    }
}
