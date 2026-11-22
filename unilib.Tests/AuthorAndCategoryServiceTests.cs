using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class AuthorServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly AuthorService _authors;

    public AuthorServiceTests()
    {
        _authors = new AuthorService(_db.Create);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Create_TrimsNamesAndAllowsEmptyFirstName()
    {
        var author = await _authors.CreateAsync(new Author { FirstName = "  ", LastName = "  Shevchenko " });

        var stored = await _authors.GetByIdAsync(author.Id);
        Assert.NotNull(stored);
        Assert.Equal("", stored.FirstName);
        Assert.Equal("Shevchenko", stored.LastName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithoutLastName_Throws(string lastName)
    {
        await Assert.ThrowsAsync<ServiceException>(() =>
            _authors.CreateAsync(new Author { FirstName = "Taras", LastName = lastName }));
        Assert.Empty(await _authors.GetAllAsync());
    }

    [Fact]
    public async Task GetAll_SortsByLastNameThenFirstName()
    {
        await _authors.CreateAsync(new Author { FirstName = "Lesya", LastName = "Ukrainka" });
        await _authors.CreateAsync(new Author { FirstName = "Ivan", LastName = "Franko" });
        await _authors.CreateAsync(new Author { FirstName = "Andriy", LastName = "Franko" });

        var names = (await _authors.GetAllAsync()).Select(a => $"{a.FirstName} {a.LastName}");

        Assert.Equal(new[] { "Andriy Franko", "Ivan Franko", "Lesya Ukrainka" }, names);
    }

    [Fact]
    public async Task Update_ChangesNames_AndValidates()
    {
        var author = await _authors.CreateAsync(new Author { FirstName = "Ivan", LastName = "Franko" });

        await _authors.UpdateAsync(new Author { Id = author.Id, FirstName = "Ivan Y.", LastName = "Franko " });
        Assert.Equal("Ivan Y.", (await _authors.GetByIdAsync(author.Id))!.FirstName);

        await Assert.ThrowsAsync<ServiceException>(() =>
            _authors.UpdateAsync(new Author { Id = author.Id, FirstName = "Ivan", LastName = "" }));
        Assert.Equal("Franko", (await _authors.GetByIdAsync(author.Id))!.LastName);
    }

    [Fact]
    public async Task UpdateOrDelete_UnknownAuthor_Throws()
    {
        await Assert.ThrowsAsync<ServiceException>(() =>
            _authors.UpdateAsync(new Author { Id = 999, LastName = "Nobody" }));
        await Assert.ThrowsAsync<ServiceException>(() => _authors.DeleteAsync(999));
    }

    [Fact]
    public async Task GetBooks_ReturnsTheAuthorsBooksByTitle()
    {
        _db.AddBook("111", title: "Zakhar Berkut");
        _db.AddBook("222", title: "Boryslav Laughs");
        _db.AddBook("333", title: "Kobzar");
        var franko = await _authors.CreateAsync(new Author { FirstName = "Ivan", LastName = "Franko" });
        var books = new BookService(_db.Create);
        await books.AddAuthorAsync("111", franko.Id);
        await books.AddAuthorAsync("222", franko.Id);

        var titles = (await _authors.GetBooksAsync(franko.Id)).Select(b => b.Title);

        Assert.Equal(new[] { "Boryslav Laughs", "Zakhar Berkut" }, titles);
    }

    [Fact]
    public async Task Delete_RemovesAuthorButKeepsTheBook()
    {
        _db.AddBook("111");
        var author = await _authors.CreateAsync(new Author { LastName = "Franko" });
        await new BookService(_db.Create).AddAuthorAsync("111", author.Id);

        await _authors.DeleteAsync(author.Id);

        Assert.Null(await _authors.GetByIdAsync(author.Id));
        var book = await new BookService(_db.Create).GetByIsbnAsync("111");
        Assert.NotNull(book);
        Assert.Empty(book.BookAuthors);
    }
}

public class CategoryServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly CategoryService _categories;

    public CategoryServiceTests()
    {
        _categories = new CategoryService(_db.Create);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Create_TrimsName_AndGetAllSortsByName()
    {
        await _categories.CreateAsync(new Category { Name = " Physics " });
        await _categories.CreateAsync(new Category { Name = "Algebra" });

        var names = (await _categories.GetAllAsync()).Select(c => c.Name);

        Assert.Equal(new[] { "Algebra", "Physics" }, names);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Create_WithoutName_Throws(string name)
    {
        await Assert.ThrowsAsync<ServiceException>(() => _categories.CreateAsync(new Category { Name = name }));
    }

    [Fact]
    public async Task Create_TooLongName_Throws()
    {
        var name = new string('x', Validation.CategoryLength + 1);

        await Assert.ThrowsAsync<ServiceException>(() => _categories.CreateAsync(new Category { Name = name }));
    }

    [Fact]
    public async Task Update_RenamesCategory()
    {
        var category = await _categories.CreateAsync(new Category { Name = "Algebra" });

        await _categories.UpdateAsync(new Category { Id = category.Id, Name = "Linear Algebra" });

        Assert.Equal("Linear Algebra", (await _categories.GetByIdAsync(category.Id))!.Name);
    }

    [Fact]
    public async Task Delete_UnusedCategory_RemovesIt()
    {
        var category = await _categories.CreateAsync(new Category { Name = "Algebra" });

        await _categories.DeleteAsync(category.Id);

        Assert.Null(await _categories.GetByIdAsync(category.Id));
    }

    [Fact]
    public async Task Delete_CategoryWithBooks_IsRefusedAndKeepsTheBooks()
    {
        var book = _db.AddBook("111", category: "Programming");

        await Assert.ThrowsAsync<ServiceException>(() => _categories.DeleteAsync(book.CategoryId));

        Assert.NotNull(await _categories.GetByIdAsync(book.CategoryId));
        Assert.NotNull(await new BookService(_db.Create).GetByIsbnAsync("111"));
    }
}
