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
}
