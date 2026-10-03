using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

// Book loans (table user_book).
public class LoanService : ServiceBase
{
    public LoanService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

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

    public async Task<UserBook> CreateAsync(int userId, string isbn, DateOnly dueDate)
    {
        using var db = CreateDb();
        if (!await db.Users.AnyAsync(u => u.Id == userId))
            throw NotFound<User>(userId);
        if (!await db.Books.AnyAsync(b => b.Isbn == isbn))
            throw NotFound<Book>(isbn);

        var loan = new UserBook
        {
            UserId = userId,
            BookIsbn = isbn,
            BorrowDate = DateOnly.FromDateTime(DateTime.Today),
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

        loan.ReturnDate = DateOnly.FromDateTime(DateTime.Today);
        loan.Status = LoanStatus.Returned;
        await SaveAsync(db, "loan");
    }

    public Task DeleteAsync(int id) => DeleteEntityAsync<UserBook>(id);

    private static IQueryable<UserBook> WithDetails(IQueryable<UserBook> loans) =>
        loans
            .Include(ub => ub.Book)
            .Include(ub => ub.User);
}
