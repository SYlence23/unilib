using System.Drawing;
using unilib.Models;
using unilib.Services;

namespace unilib.UI.Controls;

// 03 — Campus Map: pick a library and a title or shelf, and see the route from the entrance.
public class MapControl : UserControl
{
    private const int AsideWidth = 260;

    private readonly Book? _locate;
    private CatalogData _data = new();
    private int? _libraryId;
    private string? _shelf;

    private readonly FlowLayoutPanel _aside;
    private readonly Panel _main;
    private readonly Label _libraryAddress;
    private readonly Label _libraryName;
    private readonly Label _routeText;
    private readonly FloorPlanPanel _plan;

    // Opens on the given book's library and shelf, or on the first library.
    public MapControl(Book? locate = null)
    {
        _locate = locate;
        BackColor = Theme.Ground;

        var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Theme.Ground };
        header.Controls.Add(Ui.PageHeader("03 — Map", "Campus Map"));

        var body = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Ground };

        _aside = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = AsideWidth,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Theme.Ground
        };

        _main = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Ground, Padding = new Padding(24, 20, 0, 0) };
        var info = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Theme.Ground };
        _libraryAddress = Ui.Text("", 12, FontStyle.Regular, Theme.TextMuted);
        _libraryName = Ui.Text("", 25, FontStyle.Bold);
        _libraryName.Location = new Point(0, 20);
        _routeText = new Label
        {
            AutoSize = false,
            Size = new Size(420, 48),
            Font = Theme.Font(14),
            ForeColor = Theme.Ink,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.BottomRight,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        info.Controls.AddRange(new Control[] { _libraryAddress, _libraryName, _routeText });
        info.Resize += (s, e) => _routeText.Location = new Point(info.ClientSize.Width - _routeText.Width, 16);

        _plan = new FloorPlanPanel { Dock = DockStyle.Fill };
        _plan.ShelfClicked += (s, code) => SelectShelf(code);

        var legend = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 40, BackColor = Theme.Ground, Padding = new Padding(0, 12, 0, 0) };
        legend.Controls.Add(LegendItem(Theme.Neutral400, "Shelf with catalogued titles"));
        legend.Controls.Add(LegendItem(Theme.Accent, "Selected shelf"));
        legend.Controls.Add(Ui.Text("Shelves are arranged by category.", 12, FontStyle.Regular, Theme.TextMuted));

        _main.Controls.Add(_plan);
        _main.Controls.Add(info);
        _main.Controls.Add(legend);
        _main.Controls.Add(Ui.Rule(DockStyle.Left));

        body.Controls.Add(_main);
        body.Controls.Add(_aside);
        body.Controls.Add(Ui.Rule());

        Controls.Add(body);
        Controls.Add(header);

        LoadData();
    }

    private static Control LegendItem(Color swatch, string text)
    {
        var item = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0, 0, 20, 0) };
        item.Controls.Add(new Panel { BackColor = swatch, Size = new Size(12, 12), Margin = new Padding(0, 2, 6, 0) });
        item.Controls.Add(Ui.Text(text, 12, FontStyle.Regular, Theme.TextMuted));
        return item;
    }

    private async void LoadData()
    {
        try
        {
            _data = await CatalogData.LoadAsync(Session.CurrentUser!.Id);
            if (IsDisposed)
                return;
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                ShowMessage("Could not load the map.", ex.Message);
            return;
        }

        if (_data.Libraries.Count == 0)
        {
            ShowMessage("No libraries yet.", "Libraries added to the database appear here with their floor plan.");
            return;
        }

        if (_locate?.LibraryId is { } libraryId && _data.Libraries.Any(l => l.Id == libraryId))
        {
            _libraryId = libraryId;
            _shelf = _data.Shelf(_locate);
        }
        else
        {
            _libraryId = _data.Libraries[0].Id;
        }
        Render();
    }

    private void ShowMessage(string title, string text)
    {
        _main.Controls.Clear();
        _main.Controls.Add(Ui.EmptyState(title, text));
    }

    private List<Book> BooksInLibrary() => _data.Books
        .Where(b => b.LibraryId == _libraryId)
        .OrderBy(b => _data.Shelf(b))
        .ThenBy(b => b.Title)
        .ToList();

    private void SelectLibrary(int libraryId)
    {
        _libraryId = libraryId;
        _shelf = null;
        Render();
    }

    private void SelectShelf(string code)
    {
        _shelf = code;
        Render();
    }

    private void Render()
    {
        var library = _data.Libraries.First(l => l.Id == _libraryId);
        var books = BooksInLibrary();

        _aside.SuspendLayout();
        while (_aside.Controls.Count > 0)
            _aside.Controls[0].Dispose();

        _aside.Controls.Add(SectionLabel("Libraries"));
        foreach (var lib in _data.Libraries)
        {
            var count = _data.Books.Count(b => b.LibraryId == lib.Id);
            var row = new NavButton(lib.Name, detail: count.ToString())
            {
                Size = new Size(AsideWidth - 20, 44),
                TextIndent = 16,
                TextFont = Theme.Font(14, FontStyle.Bold),
                Active = lib.Id == _libraryId,
                Margin = new Padding(0)
            };
            var id = lib.Id;
            // Deferred: Render disposes the row whose Click is still running.
            row.Click += (s, e) => BeginInvoke(() => SelectLibrary(id));
            _aside.Controls.Add(row);
        }

        _aside.Controls.Add(SectionLabel("In this library"));
        if (books.Count == 0)
            _aside.Controls.Add(new Label
            {
                Text = "No catalogued titles.",
                Font = Theme.Font(13),
                ForeColor = Theme.TextMuted,
                AutoSize = true,
                Margin = new Padding(16, 4, 0, 0)
            });
        foreach (var book in books)
        {
            var shelf = _data.Shelf(book)!;
            var row = new NavButton(book.Title, number: shelf)
            {
                Size = new Size(AsideWidth - 20, 34),
                TextIndent = 16,
                TextFont = Theme.Font(13, FontStyle.Bold),
                Style = NavButton.Highlight.Tint,
                ShowRule = false,
                Active = shelf == _shelf,
                Margin = new Padding(0)
            };
            row.Click += (s, e) => BeginInvoke(() => SelectShelf(shelf));
            _aside.Controls.Add(row);
        }
        _aside.ResumeLayout();

        _libraryAddress.Text = library.Address ?? "Campus library";
        _libraryName.Text = library.Name;

        var onShelf = books.Where(b => _data.Shelf(b) == _shelf).Select(b => b.Title).ToList();
        _routeText.Text = _shelf == null
            ? "Pick a title or click a shelf to draw the route from the entrance."
            : $"Shelf {_shelf} · " + (onShelf.Count > 0 ? string.Join(", ", onShelf) : "no catalogued titles");

        _plan.Occupied = books.Select(b => _data.Shelf(b)!).ToHashSet();
        _plan.Target = _shelf;
    }

    private static Label SectionLabel(string text)
    {
        var label = Ui.Overline(text);
        label.Font = Theme.Font(11, FontStyle.Bold);
        label.Margin = new Padding(16, 16, 0, 8);
        return label;
    }
}
