using System.Drawing;
using MaterialSkin.Controls;
using unilib.Models;
using unilib.Services;

namespace unilib.UI.Controls;

// 01 — Catalog & Discovery: search, category filter, and the books as cards or a list.
public class CatalogControl : UserControl
{
    // Up to this many categories fit as segmented buttons; more use a drop-down.
    private const int MaxSegmentedCategories = 5;

    private readonly User _user;
    private CatalogData _data = new();
    private string _query = "";
    private int? _categoryId;
    private bool _showList;

    private readonly Label _resultLabel;
    private readonly MaterialTextBox2 _search;
    private readonly FlowLayoutPanel _filters;
    private readonly Panel _body;

    public CatalogControl()
    {
        _user = Session.CurrentUser!;
        BackColor = Theme.Ground;

        var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Theme.Ground };
        header.Controls.Add(Ui.PageHeader("01 — Catalog", "Catalog & Discovery"));
        _resultLabel = Ui.Text("", 14, FontStyle.Regular, Theme.TextMuted);
        _resultLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        header.Controls.Add(_resultLabel);
        header.Resize += (s, e) => PlaceResultLabel(header);
        _resultLabel.SizeChanged += (s, e) => PlaceResultLabel(header);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Theme.Ground };
        toolbar.Controls.Add(Ui.Rule());
        _filters = new FlowLayoutPanel
        {
            Location = new Point(0, 20),
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        _search = Ui.TextField("Search books...", 320);
        _search.LeadingIcon = Icons.Bitmap(Glyph.Search, Color.Black);
        _search.Margin = new Padding(0, 0, 8, 0);
        _search.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            ApplySearch();
        };
        // Clearing the field clears the search, as in the design.
        _search.TextChanged += (s, e) =>
        {
            if (_search.Text.Length == 0 && _query.Length > 0)
                ApplySearch();
        };
        var btnSearch = Ui.Button("Search", MaterialButton.MaterialButtonType.Contained);
        btnSearch.Margin = new Padding(0, 6, 24, 0);
        btnSearch.Click += (s, e) => ApplySearch();
        _filters.Controls.Add(_search);
        _filters.Controls.Add(btnSearch);
        toolbar.Controls.Add(_filters);

        var view = new SegmentedButtons(new[] { "Grid", "List" }) { Anchor = AnchorStyles.Top | AnchorStyles.Right };
        view.SelectedIndexChanged += (s, e) =>
        {
            _showList = view.SelectedIndex == 1;
            Render();
        };
        toolbar.Controls.Add(view);
        toolbar.Resize += (s, e) => view.Location = new Point(toolbar.ClientSize.Width - view.Width, 26);

        _body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Ground, Padding = new Padding(0, 16, 0, 0) };

        // The last control added is docked first: header, then toolbar, then the body below.
        Controls.Add(_body);
        Controls.Add(toolbar);
        Controls.Add(header);

        LoadData();
    }

    // Raised when the user wants to see a book on the campus map.
    public event EventHandler<Book>? LocateRequested;

    private void PlaceResultLabel(Control header) =>
        _resultLabel.Location = new Point(header.ClientSize.Width - _resultLabel.Width, header.Height - _resultLabel.Height - 12);

    private async void LoadData()
    {
        try
        {
            _data = await CatalogData.LoadAsync(_user.Id);
            if (IsDisposed)
                return;
            AddCategoryFilter();
            Render();
        }
        catch (Exception ex)
        {
            if (IsDisposed)
                return;
            ShowInBody(Ui.EmptyState("Could not load the catalog.", ex.Message));
        }
    }

    private void AddCategoryFilter()
    {
        var names = new[] { "All Categories" }.Concat(_data.Categories.Select(c => c.Name)).ToList();
        void Pick(int index) { _categoryId = index == 0 ? null : _data.Categories[index - 1].Id; Render(); }

        if (_data.Categories.Count <= MaxSegmentedCategories)
        {
            var segmented = new SegmentedButtons(names) { Margin = new Padding(0, 6, 0, 0) };
            segmented.SelectedIndexChanged += (s, e) => Pick(segmented.SelectedIndex);
            _filters.Controls.Add(segmented);
        }
        else
        {
            var combo = new MaterialComboBox { Width = 220, Hint = "Category", MaxDropDownItems = 10, Margin = new Padding(0) };
            combo.Items.AddRange(names.ToArray<object>());
            combo.SelectedIndex = 0;
            combo.SelectedIndexChanged += (s, e) => Pick(Math.Max(combo.SelectedIndex, 0));
            _filters.Controls.Add(combo);
        }
    }

    private void ApplySearch()
    {
        _query = _search.Text.Trim();
        Render();
    }

    private void ClearFilters()
    {
        _search.Text = "";
        _query = "";
        foreach (Control control in _filters.Controls)
        {
            if (control is SegmentedButtons segmented) segmented.SelectedIndex = 0;
            if (control is MaterialComboBox combo) combo.SelectedIndex = 0;
        }
        _categoryId = null;
        Render();
    }

    private List<Book> FilteredBooks()
    {
        var q = _query.ToLowerInvariant();
        return _data.Books
            .Where(b => _categoryId == null || b.CategoryId == _categoryId)
            .Where(b => q.Length == 0
                || b.Title.ToLowerInvariant().Contains(q)
                || b.Isbn.ToLowerInvariant().Contains(q)
                || CatalogData.Authors(b).ToLowerInvariant().Contains(q))
            .ToList();
    }

    private void Render()
    {
        var books = FilteredBooks();
        _resultLabel.Text = $"{books.Count} of {_data.Books.Count} titles";

        if (_data.Books.Count == 0)
            ShowInBody(Ui.EmptyState("The catalog is empty.", "Books added in Admin Control appear here."));
        else if (books.Count == 0)
            ShowInBody(Ui.EmptyState("No books found.", null, "Clear search and filters", ClearFilters));
        else
            ShowInBody(_showList ? BuildList(books) : BuildGrid(books));
    }

    private void ShowInBody(Control control)
    {
        _body.SuspendLayout();
        while (_body.Controls.Count > 0)
            _body.Controls[0].Dispose();
        control.Dock = DockStyle.Fill;
        _body.Controls.Add(control);
        _body.ResumeLayout();
    }

    private Control BuildGrid(List<Book> books)
    {
        var grid = new FlowLayoutPanel { AutoScroll = true, BackColor = Theme.Ground };
        grid.SuspendLayout();
        foreach (var book in books)
            grid.Controls.Add(BuildCard(book));
        grid.ResumeLayout();
        return grid;
    }

    private Control BuildCard(Book book)
    {
        const int inner = 204;
        var card = new MaterialCard { Size = new Size(236, 300), Margin = new Padding(0, 0, 16, 16), Padding = new Padding(0) };

        var category = Ui.Text(book.Category.Name.ToUpperInvariant(), 10, FontStyle.Bold, Theme.Accent700);
        category.Location = new Point(16, 18);
        card.Controls.Add(category);

        var btnSave = new IconButton(_data.IsSaved(book.Isbn) ? Glyph.BookmarkFilled : Glyph.Bookmark, "Save", 30)
        {
            Location = new Point(236 - 16 - 30, 10)
        };
        btnSave.IconColor = _data.IsSaved(book.Isbn) ? Theme.Accent : Theme.Ink;
        btnSave.Click += async (s, e) =>
        {
            var saved = await ToggleSaved(book);
            if (saved == null || btnSave.IsDisposed)
                return;
            btnSave.IconColor = saved.Value ? Theme.Accent : Theme.Ink;
            btnSave.Glyph = saved.Value ? Glyph.BookmarkFilled : Glyph.Bookmark;
        };
        card.Controls.Add(btnSave);

        var title = new Label
        {
            Text = book.Title,
            Font = Theme.Font(19, FontStyle.Bold),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent,
            Bounds = new Rectangle(16, 44, inner, 52),
            AutoEllipsis = true
        };
        card.Controls.Add(title);

        card.Controls.Add(CardLine(CatalogData.Authors(book), 13, 98));
        card.Controls.Add(CardLine($"ISBN: {book.Isbn}", 12, 138));
        card.Controls.Add(CardLine(_data.Location(book), 12, 156));

        var available = _data.Available(book);
        card.Controls.Add(new Chip(_data.AvailabilityLabel(book), available > 0 ? ChipKind.Neutral : ChipKind.Outline)
        {
            Location = new Point(16, 186)
        });

        var btnReserve = Ui.Button("Reserve", MaterialButton.MaterialButtonType.Contained, dense: true);
        btnReserve.AutoSize = false;
        btnReserve.Bounds = new Rectangle(16, 244, 104, 36);
        SetReserveState(btnReserve, book);
        btnReserve.Click += async (s, e) =>
        {
            await Reserve(book, btnReserve);
            if (!btnReserve.IsDisposed)
                SetReserveState(btnReserve, book);
        };
        card.Controls.Add(btnReserve);

        var btnLocate = Ui.Button("Locate", MaterialButton.MaterialButtonType.Outlined, Glyph.Pin, dense: true);
        btnLocate.AutoSize = false;
        btnLocate.Bounds = new Rectangle(128, 244, 92, 36);
        btnLocate.Enabled = book.LibraryId != null;
        btnLocate.Click += (s, e) => LocateRequested?.Invoke(this, book);
        card.Controls.Add(btnLocate);

        return card;
    }

    private static Label CardLine(string text, float px, int y) => new()
    {
        Text = text,
        Font = Theme.Font(px),
        ForeColor = Theme.TextMuted,
        BackColor = Color.Transparent,
        Bounds = new Rectangle(16, y, 204, (int)px + 6),
        AutoEllipsis = true
    };

    private void SetReserveState(MaterialButton button, Book book)
    {
        var held = _data.IsHeld(book.Isbn);
        button.Text = held ? "Reserved" : "Reserve";
        button.Enabled = !held;
    }

    private Control BuildList(List<Book> books)
    {
        var panel = new Panel { BackColor = Theme.Ground };

        var list = Ui.Table(("Title", 2.4f), ("Author", 1.6f), ("Category", 1f), ("ISBN", 1.1f),
            ("Location", 1.7f), ("Availability", 1.1f), ("Status", 0.8f));
        foreach (var book in books)
        {
            var item = new ListViewItem(new[]
            {
                book.Title, CatalogData.Authors(book), book.Category.Name, book.Isbn,
                _data.Location(book), _data.AvailabilityLabel(book), StatusText(book)
            }) { Tag = book };
            list.Items.Add(item);
        }

        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.Ground, WrapContents = false };
        var btnReserve = Ui.Button("Reserve", MaterialButton.MaterialButtonType.Contained);
        var btnLocate = Ui.Button("Locate", MaterialButton.MaterialButtonType.Outlined, Glyph.Pin);
        var btnSave = Ui.Button("Save", MaterialButton.MaterialButtonType.Outlined, Glyph.Bookmark);
        var hint = Ui.Text("Select a book to reserve, locate or save it.", 13, FontStyle.Regular, Theme.TextMuted);
        hint.Margin = new Padding(8, 14, 0, 0);
        actions.Controls.AddRange(new Control[] { btnReserve, btnLocate, btnSave, hint });

        Book? Selected() => list.SelectedItems.Count > 0 ? (Book)list.SelectedItems[0].Tag! : null;
        void UpdateActions()
        {
            var book = Selected();
            btnReserve.Enabled = book != null && !_data.IsHeld(book.Isbn);
            btnReserve.Text = book != null && _data.IsHeld(book.Isbn) ? "Reserved" : "Reserve";
            btnLocate.Enabled = book?.LibraryId != null;
            btnSave.Enabled = book != null;
            btnSave.Text = book != null && _data.IsSaved(book.Isbn) ? "Unsave" : "Save";
        }
        void RefreshSelectedRow()
        {
            if (list.SelectedItems.Count > 0)
                list.SelectedItems[0].SubItems[6].Text = StatusText((Book)list.SelectedItems[0].Tag!);
            UpdateActions();
        }

        list.SelectedIndexChanged += (s, e) => UpdateActions();
        list.DoubleClick += (s, e) => { if (Selected() is { LibraryId: not null } book) LocateRequested?.Invoke(this, book); };
        btnReserve.Click += async (s, e) => { if (Selected() is { } book) { await Reserve(book, btnReserve); RefreshSelectedRow(); } };
        btnLocate.Click += (s, e) => { if (Selected() is { } book) LocateRequested?.Invoke(this, book); };
        btnSave.Click += async (s, e) => { if (Selected() is { } book) { await ToggleSaved(book); RefreshSelectedRow(); } };
        UpdateActions();

        // The last control added is docked first: actions on top, the list fills the rest.
        panel.Controls.Add(list);
        panel.Controls.Add(actions);
        return panel;
    }

    private string StatusText(Book book) =>
        _data.IsHeld(book.Isbn) ? "Reserved" : _data.IsSaved(book.Isbn) ? "Saved" : "";

    private async Task Reserve(Book book, MaterialButton button)
    {
        button.Enabled = false;
        try
        {
            var message = await _data.ReserveAsync(_user.Id, book);
            if (!IsDisposed)
                Ui.Toast(this, message);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                Ui.Toast(this, ex.Message);
        }
        finally
        {
            if (!button.IsDisposed)
                button.Enabled = true;
        }
    }

    // Returns whether the book is saved now, or null if saving failed.
    private async Task<bool?> ToggleSaved(Book book)
    {
        try
        {
            return await _data.ToggleSavedAsync(_user.Id, book);
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                Ui.Toast(this, ex.Message);
            return null;
        }
    }
}
