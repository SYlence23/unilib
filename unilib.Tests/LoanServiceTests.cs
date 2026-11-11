using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class LoanServiceTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 3, 10);

    private readonly TestDb _db = new();
    private readonly LoanService _loans;
    private readonly int _userId;

    public LoanServiceTests()
    {
        _loans = new LoanService(_db.Create, new FixedTime(Today));
        _db.AddBook("111", amount: 1, title: "Clean Code");
        _userId = _db.AddUser();
    }

    public void Dispose() => _db.Dispose();

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
            () => _loans.CreateAsync(_userId, "999", Today.AddDays(14)));
    }

    [Fact]
    public async Task Create_UnknownUser_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _loans.CreateAsync(9999, "111", Today.AddDays(14)));
    }

    [Fact]
    public async Task Create_NoCopiesLeft_Throws()
    {
        // Book 111 has one copy.
        await _loans.CreateAsync(_userId, "111", Today.AddDays(14));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _loans.CreateAsync(_userId, "111", Today.AddDays(14)));
        Assert.Contains("No copies", ex.Message);
    }

    [Fact]
    public async Task Create_AfterCopyReturned_Succeeds()
    {
        var first = await _loans.CreateAsync(_userId, "111", Today.AddDays(14));
        await _loans.ReturnAsync(first.Id);

        var second = await _loans.CreateAsync(_userId, "111", Today.AddDays(14));

        Assert.Equal(LoanStatus.Active, second.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task Create_DueDateNotAfterToday_Throws(int days)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _loans.CreateAsync(_userId, "111", Today.AddDays(days)));
        Assert.Empty(await _loans.GetActiveAsync());
    }

    [Fact]
    public async Task Create_OverLoanLimit_Throws()
    {
        for (var i = 0; i < LibraryRules.MaxActiveLoans; i++)
        {
            _db.AddBook($"B{i}");
            await _loans.CreateAsync(_userId, $"B{i}", Today.AddDays(14));
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _loans.CreateAsync(_userId, "111", Today.AddDays(14)));
        Assert.Contains("Loan limit", ex.Message);
    }

    [Fact]
    public async Task Create_ReturnedLoansDontCountTowardsLimit()
    {
        for (var i = 0; i < LibraryRules.MaxActiveLoans; i++)
        {
            _db.AddBook($"B{i}");
            _db.AddLoan(_userId, $"B{i}", Today.AddDays(-30), Today.AddDays(-16), returned: Today.AddDays(-20));
        }

        await _loans.CreateAsync(_userId, "111", Today.AddDays(14));
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

    [Fact]
    public async Task MarkOverdue_ChangesOnlyActiveLoansPastDueDate()
    {
        _db.AddBook("222");
        _db.AddBook("333");
        var late = _db.AddLoan(_userId, "111", Today.AddDays(-20), Today.AddDays(-6));
        var dueToday = _db.AddLoan(_userId, "222", Today.AddDays(-14), Today);
        var returned = _db.AddLoan(_userId, "333", Today.AddDays(-30), Today.AddDays(-16), returned: Today.AddDays(-18));

        Assert.Equal(1, await _loans.MarkOverdueAsync());

        Assert.Equal(LoanStatus.Overdue, (await _loans.GetByIdAsync(late.Id))!.Status);
        Assert.Equal(LoanStatus.Active, (await _loans.GetByIdAsync(dueToday.Id))!.Status);
        Assert.Equal(LoanStatus.Returned, (await _loans.GetByIdAsync(returned.Id))!.Status);
        Assert.Equal(0, await _loans.MarkOverdueAsync());
    }

    [Fact]
    public async Task Return_OverdueLoan_IsReturned()
    {
        var late = _db.AddLoan(_userId, "111", Today.AddDays(-20), Today.AddDays(-6));
        await _loans.MarkOverdueAsync();

        await _loans.ReturnAsync(late.Id);

        Assert.Equal(LoanStatus.Returned, (await _loans.GetByIdAsync(late.Id))!.Status);
    }
}
