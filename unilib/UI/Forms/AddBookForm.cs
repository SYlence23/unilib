using System.Drawing;
using System.Text.RegularExpressions;
using MaterialSkin.Controls;
using unilib.Models;
using unilib.Services;

namespace unilib.UI.Forms;

// Dialog for staff to add a book with its authors. DialogResult.OK when the book was saved.
public class AddBookForm : MaterialForm
{
    private const int FieldWidth = 512;
    private const string NewCategoryOption = "New category…";
    private const string NoLibraryOption = "No library yet";

    private readonly List<Category> _categories;
    private readonly List<Library> _libraries;

    private readonly MaterialTextBox2 _title;
    private readonly MaterialTextBox2 _authors;
    private readonly MaterialTextBox2 _isbn;
    private readonly MaterialTextBox2 _copies;
    private readonly MaterialComboBox _category;
    private readonly MaterialTextBox2 _newCategory;
    private readonly MaterialComboBox _library;
    private readonly Label _shelfInfo;
    private readonly Label _error;
    private readonly MaterialButton _btnAdd;

    public AddBookForm(List<Category> categories, List<Library> libraries)
    {
        _categories = categories;
        _libraries = libraries;

        Text = "Add New Book";
        FormStyle = FormStyles.ActionBar_48;
        Size = new Size(560, 640);
        Sizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Theme.Apply(this);
        BackColor = Theme.Ground;

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Ground,
            Padding = new Padding(21, 20, 21, 0)
        };

        _title = Ui.TextField("Title", FieldWidth);
        _authors = Ui.TextField("Author(s), separated by commas", FieldWidth);
        body.Controls.Add(_title);
        body.Controls.Add(_authors);

        _isbn = Ui.TextField("ISBN (10 or 13 digits)", 340);
        _isbn.Margin = new Padding(0, 0, 12, 12);
        _copies = Ui.TextField("Copies", FieldWidth - 352);
        _copies.Text = "1";
        body.Controls.Add(Row(_isbn, _copies));

        _category = new MaterialComboBox { Hint = "Category", Width = 250, MaxDropDownItems = 8, Margin = new Padding(0, 0, 12, 12) };
        _category.Items.AddRange(categories.Select(c => (object)c.Name).Append(NewCategoryOption).ToArray());
        _category.SelectedIndex = 0;
        _library = new MaterialComboBox { Hint = "Library", Width = FieldWidth - 262, MaxDropDownItems = 8, Margin = new Padding(0, 0, 0, 12) };
        _library.Items.AddRange(libraries.Select(l => (object)l.Name).Append(NoLibraryOption).ToArray());
        _library.SelectedIndex = 0;
        body.Controls.Add(Row(_category, _library));

        _newCategory = Ui.TextField("New category name", FieldWidth);
        _newCategory.Visible = categories.Count == 0;
        body.Controls.Add(_newCategory);

        _shelfInfo = Ui.Text("", 13, FontStyle.Regular, Theme.TextMuted);
        _shelfInfo.Margin = new Padding(0, 0, 0, 8);
        body.Controls.Add(_shelfInfo);
        _error = Ui.ErrorLabel(FieldWidth);
        body.Controls.Add(_error);

        _category.SelectedIndexChanged += (s, e) => UpdateCategoryFields();
        _library.SelectedIndexChanged += (s, e) => UpdateCategoryFields();
        UpdateCategoryFields();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Theme.Ground,
            Padding = new Padding(0, 8, 16, 0)
        };
        _btnAdd = Ui.Button("Add to catalog", MaterialButton.MaterialButtonType.Contained);
        _btnAdd.Margin = new Padding(0);
        _btnAdd.Click += BtnAdd_Click;
        var btnCancel = Ui.Button("Cancel", MaterialButton.MaterialButtonType.Text);
        btnCancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(_btnAdd);
        actions.Controls.Add(btnCancel);

        Controls.Add(body);
        Controls.Add(actions);
        AcceptButton = _btnAdd;
        CancelButton = btnCancel;
    }

    public string AddedTitle { get; private set; } = "";

    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Color.Transparent, Margin = new Padding(0) };
        row.Controls.AddRange(controls);
        return row;
    }

    private Category? SelectedCategory =>
        _category.SelectedIndex >= 0 && _category.SelectedIndex < _categories.Count ? _categories[_category.SelectedIndex] : null;

    private Library? SelectedLibrary =>
        _library.SelectedIndex >= 0 && _library.SelectedIndex < _libraries.Count ? _libraries[_library.SelectedIndex] : null;

    private void UpdateCategoryFields()
    {
        _newCategory.Visible = SelectedCategory == null;
        _shelfInfo.Text = SelectedLibrary == null
            ? "Without a library the book has no location on the map."
            : SelectedCategory == null
                ? "The shelf is assigned from the category once it is created."
                : $"Shelved by category: {SelectedLibrary.Name} · Shelf {Shelving.ShelfFor(SelectedCategory.Id, _categories)}";
    }

    // "Frank Herbert, Ursula K. Le Guin" -> (Frank, Herbert), (Ursula K. Le, Guin). A single word is a last name.
    private static List<(string First, string Last)> ParseAuthors(string text) => text
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(name =>
        {
            var split = name.LastIndexOf(' ');
            return split < 0 ? ("", name) : (name[..split].Trim(), name[(split + 1)..]);
        })
        .ToList();

    private async void BtnAdd_Click(object? sender, EventArgs e)
    {
        _error.Visible = false;
        var title = _title.Text.Trim();
        var authors = ParseAuthors(_authors.Text);
        var isbn = Regex.Replace(_isbn.Text, "[^0-9Xx]", "").ToUpperInvariant();
        var newCategory = _newCategory.Text.Trim();

        string? error = null;
        if (title.Length == 0 || authors.Count == 0)
            error = "Title and author are required.";
        else if (isbn.Length != 10 && isbn.Length != 13)
            error = "ISBN must have 10 or 13 digits.";
        else if (!int.TryParse(_copies.Text.Trim(), out var parsed) || parsed < 0)
            error = "Copies must be a whole number, 0 or more.";
        else if (SelectedCategory == null && newCategory.Length == 0)
            error = "Choose a category or enter a new one.";
        if (error != null)
        {
            Ui.ShowError(_error, error);
            return;
        }
        var copies = int.Parse(_copies.Text.Trim());

        _btnAdd.Enabled = false;
        try
        {
            var books = new BookService();
            if (await books.GetByIsbnAsync(isbn) != null)
            {
                Ui.ShowError(_error, "A book with this ISBN is already catalogued.");
                return;
            }

            var category = SelectedCategory ?? await new CategoryService().CreateAsync(new Category { Name = newCategory });
            await books.CreateAsync(new Book
            {
                Isbn = isbn,
                Title = title,
                CategoryId = category.Id,
                LibraryId = SelectedLibrary?.Id,
                Amount = copies
            });

            var authorService = new AuthorService();
            var known = await authorService.GetAllAsync();
            foreach (var (first, last) in authors)
            {
                var author = known.FirstOrDefault(a =>
                    string.Equals(a.FirstName, first, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(a.LastName, last, StringComparison.OrdinalIgnoreCase))
                    ?? await authorService.CreateAsync(new Author { FirstName = first, LastName = last });
                await books.AddAuthorAsync(isbn, author.Id);
            }

            AddedTitle = title;
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            Ui.ShowError(_error, ex.Message);
        }
        finally
        {
            if (!IsDisposed)
                _btnAdd.Enabled = true;
        }
    }
}
