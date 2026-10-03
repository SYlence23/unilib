using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class LoanServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly LoanService _loans;
    private readonly int _userId;

    public LoanServiceTests()
    {
        _loans = new LoanService(_db.Create);

        using var db = _db.Create();
        db.Books.Add(new Book { Isbn = "111", Title = "Clean Code", Category = new Category { Name = "Programming" }, Amount = 1 });
        db.SaveChanges();

        _userId = new AuthService(_db.Create)
            .RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!").GetAwaiter().GetResult().Id;
    }

    public void Dispose() => _db.Dispose();

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public async Task Create_MakesActiveLoan()
    {
        var loan = await _loans.CreateAsync(_userId, "111", Today.AddDays(14));

        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(Today, loan.BorrowDate);
        Assert.Equal(Today.AddDays(14), loan.DueDate);
        Assert.Null(loan.ReturnDate);
        Assert.Single(await _loans.GetActiveAsync());
    }

    [Fact]
    public async Task Create_UnknownBook_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _loans.CreateAsync(_userId, "999", Today));
    }

    [Fact]
    public async Task Return_SetsReturnDateAndStatus()
    {
        var loan = await _loans.CreateAsync(_userId, "111", Today.AddDays(14));

        await _loans.ReturnAsync(loan.Id);

        var returned = await _loans.GetByIdAsync(loan.Id);
        Assert.Equal(LoanStatus.Returned, returned!.Status);
        Assert.Equal(Today, returned.ReturnDate);
        Assert.Empty(await _loans.GetActiveAsync());
    }

    [Fact]
    public async Task Return_AlreadyReturned_Throws()
    {
        var loan = await _loans.CreateAsync(_userId, "111", Today.AddDays(14));
        await _loans.ReturnAsync(loan.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _loans.ReturnAsync(loan.Id));
    }

    [Fact]
    public async Task GetByUser_IncludesBook()
    {
        await _loans.CreateAsync(_userId, "111", Today.AddDays(14));

        var loan = Assert.Single(await _loans.GetByUserAsync(_userId));
        Assert.Equal("Clean Code", loan.Book.Title);
    }
}
