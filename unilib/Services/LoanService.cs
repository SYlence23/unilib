using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

// Book loans (table user_book).
public class LoanService : ServiceBase
{
    private readonly TimeProvider _time;

    // time: tests pass a fixed clock; the app uses the system clock.
    public LoanService(Func<LibraryDbContext>? createDb = null, TimeProvider? time = null) : base(createDb)
    {
        _time = time ?? TimeProvider.System;
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetLocalNow().DateTime);

    public async Task<List<UserBook>> GetAllAsync()
    {
        using var db = CreateDb();
        return await WithDetails(db.UserBooks.AsNoTracking())
            .OrderByDescending(ub => ub.BorrowDate)
            .ToListAsync();
    }

    public async Task<UserBook?> GetByIdAsync(int id)
    {
        using var db = CreateDb();
        return await WithDetails(db.UserBooks.AsNoTracking()).SingleOrDefaultAsync(ub => ub.Id == id);
    }

    public async Task<List<UserBook>> GetByUserAsync(int userId)
    {
        using var db = CreateDb();
        return await WithDetails(db.UserBooks.AsNoTracking())
            .Where(ub => ub.UserId == userId)
            .OrderByDescending(ub => ub.BorrowDate)
            .ToListAsync();
    }

    // Loans that have not been returned yet.
    public async Task<List<UserBook>> GetActiveAsync()
    {
        using var db = CreateDb();
        return await WithDetails(db.UserBooks.AsNoTracking())
            .Where(ub => ub.ReturnDate == null)
            .OrderBy(ub => ub.DueDate)
            .ToListAsync();
    }

    // Refused when the due date isn't after today, no copy is free, or the user
    // already has LibraryRules.MaxActiveLoans books.
    public async Task<UserBook> CreateAsync(int userId, string isbn, DateOnly dueDate)
    {
        var today = Today;
        if (dueDate <= today)
            throw new InvalidOperationException("The due date must be after today.");

        using var db = CreateDb();
        if (!await db.Users.AnyAsync(u => u.Id == userId))
            throw NotFound<User>(userId);
        var amount = await db.Books.Where(b => b.Isbn == isbn).Select(b => (int?)(b.Amount ?? 0)).SingleOrDefaultAsync()
            ?? throw NotFound<Book>(isbn);

        var onLoan = await db.UserBooks.CountAsync(ub => ub.BookIsbn == isbn && ub.ReturnDate == null);
        if (onLoan >= amount)
            throw new InvalidOperationException("No copies of this book are available.");

        var userLoans = await db.UserBooks.CountAsync(ub => ub.UserId == userId && ub.ReturnDate == null);
        if (userLoans >= LibraryRules.MaxActiveLoans)
            throw new InvalidOperationException($"Loan limit reached ({LibraryRules.MaxActiveLoans} books on loan).");

        var loan = new UserBook
        {
            UserId = userId,
            BookIsbn = isbn,
            BorrowDate = today,
            DueDate = dueDate,
            Status = LoanStatus.Active
        };

        db.UserBooks.Add(loan);
        await SaveAsync(db, "loan");
        return loan;
    }

    public async Task ReturnAsync(int loanId)
    {
        using var db = CreateDb();
        var loan = await db.UserBooks.FindAsync(loanId) ?? throw NotFound<UserBook>(loanId);

        if (loan.ReturnDate != null)
            throw new InvalidOperationException("This book has already been returned.");

        loan.ReturnDate = Today;
        loan.Status = LoanStatus.Returned;
        await SaveAsync(db, "loan");
    }

    // Marks active loans past their due date as overdue. Returns how many changed.
    public async Task<int> MarkOverdueAsync()
    {
        var today = Today;
        using var db = CreateDb();
        var late = await db.UserBooks
            .Where(ub => ub.ReturnDate == null && ub.Status == LoanStatus.Active && ub.DueDate < today)
            .ToListAsync();
        foreach (var loan in late)
            loan.Status = LoanStatus.Overdue;
        if (late.Count > 0)
            await SaveAsync(db, "loan");
        return late.Count;
    }

    public Task DeleteAsync(int id) => DeleteEntityAsync<UserBook>(id);

    private static IQueryable<UserBook> WithDetails(IQueryable<UserBook> loans) =>
        loans
            .Include(ub => ub.Book)
            .Include(ub => ub.User);
}
