using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class BookServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly BookService _books;

    public BookServiceTests()
    {
        _books = new BookService(_db.Create);

        using var db = _db.Create();
        var category = new Category { Name = "Programming" };
        var author = new Author { FirstName = "Robert", LastName = "Martin" };
        db.AddRange(category, author);
        db.Books.Add(new Book { Isbn = "111", Title = "Clean Code", Category = category, Amount = 2 });
        db.Books.Add(new Book { Isbn = "222", Title = "Algorithms", Category = category, Amount = 1 });
        db.BookAuthors.Add(new BookAuthor { BookId = "111", Author = author });
        db.SaveChanges();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetByIsbn_IncludesCategoryAndAuthors()
    {
        var book = await _books.GetByIsbnAsync("111");

        Assert.NotNull(book);
        Assert.Equal("Programming", book.Category.Name);
        Assert.Equal("Martin", Assert.Single(book.BookAuthors).Author.LastName);
    }

    [Fact]
    public async Task GetByIsbn_Missing_ReturnsNull()
    {
        Assert.Null(await _books.GetByIsbnAsync("999"));
    }

    [Theory]
    [InlineData("clean", "111")]
    [InlineData("martin", "111")]
    [InlineData("222", "222")]
    public async Task Search_MatchesTitleAuthorOrIsbn(string text, string expectedIsbn)
    {
        var results = await _books.SearchAsync(text);

        Assert.Equal(expectedIsbn, Assert.Single(results).Isbn);
    }

    [Fact]
    public async Task GetAvailableCopies_SubtractsOpenLoans()
    {
        var user = await new AuthService(_db.Create).RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!");
        await new LoanService(_db.Create).CreateAsync(user.Id, "111", DateOnly.FromDateTime(DateTime.Today.AddDays(14)));

        Assert.Equal(1, await _books.GetAvailableCopiesAsync("111"));
    }

    [Fact]
    public async Task AddAndRemoveAuthor_UpdatesLinks()
    {
        var author = await new AuthorService(_db.Create).CreateAsync(new Author { FirstName = "Donald", LastName = "Knuth" });

        await _books.AddAuthorAsync("222", author.Id);
        Assert.Single((await _books.GetByIsbnAsync("222"))!.BookAuthors);

        await _books.RemoveAuthorAsync("222", author.Id);
        Assert.Empty((await _books.GetByIsbnAsync("222"))!.BookAuthors);
    }

    [Fact]
    public async Task AddWithAuthors_ReusesKnownAuthorAndCreatesNewOnes()
    {
        var categoryId = (await new CategoryService(_db.Create).GetAllAsync()).Single().Id;

        await _books.AddWithAuthorsAsync(new Book { Isbn = "333", Title = "Refactoring", CategoryId = categoryId },
            null, new[] { ("robert", "MARTIN"), ("Martin", "Fowler"), ("Martin", "Fowler") });

        var book = await _books.GetByIsbnAsync("333");
        Assert.NotNull(book);
        Assert.Equal(new[] { "Fowler", "Martin" }, book.BookAuthors.Select(ba => ba.Author.LastName).OrderBy(n => n));
        Assert.Equal(2, (await new AuthorService(_db.Create).GetAllAsync()).Count);
    }

    [Fact]
    public async Task AddWithAuthors_NewCategoryName_ReusesExistingIgnoringCase()
    {
        await _books.AddWithAuthorsAsync(new Book { Isbn = "333", Title = "SICP" }, " programming ", new[] { ("", "Abelson") });

        Assert.Equal("Programming", (await _books.GetByIsbnAsync("333"))!.Category.Name);
        Assert.Single(await new CategoryService(_db.Create).GetAllAsync());
    }

    [Fact]
    public async Task AddWithAuthors_DuplicateIsbn_StoresNothing()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _books.AddWithAuthorsAsync(new Book { Isbn = "111", Title = "Copy" }, "Fiction", new[] { ("Frank", "Herbert") }));

        Assert.Contains("already catalogued", ex.Message);
        Assert.Single(await new CategoryService(_db.Create).GetAllAsync());
        Assert.Single(await new AuthorService(_db.Create).GetAllAsync());
    }

    [Fact]
    public async Task AddWithAuthors_FailedSave_StoresNoCategoryOrAuthors()
    {
        // CategoryId 999 doesn't exist, so the save fails on the foreign key.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _books.AddWithAuthorsAsync(new Book { Isbn = "444", Title = "Broken", CategoryId = 999 }, null, new[] { ("Frank", "Herbert") }));

        Assert.Null(await _books.GetByIsbnAsync("444"));
        Assert.Single(await new AuthorService(_db.Create).GetAllAsync());
    }

    [Fact]
    public async Task AddWithAuthors_FailedSaveWithNewCategory_StoresNoCategoryAndRetryWorks()
    {
        // LibraryId 999 doesn't exist, so the save fails after the new category was prepared.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _books.AddWithAuthorsAsync(new Book { Isbn = "444", Title = "Dune", LibraryId = 999 }, "Fiction", new[] { ("Frank", "Herbert") }));

        Assert.Single(await new CategoryService(_db.Create).GetAllAsync());
        Assert.Single(await new AuthorService(_db.Create).GetAllAsync());

        // The original bug: retrying failed on the category's unique name.
        await _books.AddWithAuthorsAsync(new Book { Isbn = "444", Title = "Dune" }, "Fiction", new[] { ("Frank", "Herbert") });
        Assert.Equal("Fiction", (await _books.GetByIsbnAsync("444"))!.Category.Name);
    }

    [Fact]
    public async Task AddWithAuthors_MatchesCyrillicNamesIgnoringCase()
    {
        await _books.AddWithAuthorsAsync(new Book { Isbn = "555", Title = "Кобзар" }, "Поезія", new[] { ("Тарас", "Шевченко") });
        await _books.AddWithAuthorsAsync(new Book { Isbn = "556", Title = "Гайдамаки" }, "ПОЕЗІЯ", new[] { ("тарас", "шевченко") });

        Assert.Equal(2, (await new CategoryService(_db.Create).GetAllAsync()).Count);
        Assert.Equal(2, (await new AuthorService(_db.Create).GetAllAsync()).Count);
    }
}
