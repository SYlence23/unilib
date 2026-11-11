using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

// Short ways to put the rows a test needs into a TestDb.
public static class TestData
{
    // Adds a book, reusing the category with this name if it already exists.
    public static Book AddBook(this TestDb testDb, string isbn, int? amount = 1,
        string category = "Programming", int? libraryId = null, string? title = null)
    {
        using var db = testDb.Create();
        var book = new Book
        {
            Isbn = isbn,
            Title = title ?? $"Book {isbn}",
            Amount = amount,
            LibraryId = libraryId,
            CategoryId = db.Categories.SingleOrDefault(c => c.Name == category)?.Id ?? 0
        };
        if (book.CategoryId == 0)
            book.Category = new Category { Name = category };
        db.Books.Add(book);
        db.SaveChanges();
        return book;
    }

    public static Library AddLibrary(this TestDb testDb, string name = "Central")
    {
        using var db = testDb.Create();
        var library = new Library { Name = name };
        db.Libraries.Add(library);
        db.SaveChanges();
        return library;
    }

    // Registers a student and returns its id.
    public static int AddUser(this TestDb testDb, string email = "ivan@uni.edu", string password = "Secret123!") =>
        new AuthService(testDb.Create).RegisterAsync("Ivan", "Petrenko", email, password)
            .GetAwaiter().GetResult().Id;

    // Writes a loan straight to the database, skipping LoanService's rules.
    public static UserBook AddLoan(this TestDb testDb, int userId, string isbn, DateOnly borrowed, DateOnly due,
        DateOnly? returned = null)
    {
        using var db = testDb.Create();
        var loan = new UserBook
        {
            UserId = userId,
            BookIsbn = isbn,
            BorrowDate = borrowed,
            DueDate = due,
            ReturnDate = returned,
            Status = returned == null ? LoanStatus.Active : LoanStatus.Returned
        };
        db.UserBooks.Add(loan);
        db.SaveChanges();
        return loan;
    }

    // Writes a reservation with any status straight to the database.
    public static Reservation AddReservation(this TestDb testDb, int userId, string isbn, string status, DateOnly date)
    {
        using var db = testDb.Create();
        var reservation = new Reservation { UserId = userId, BookId = isbn, Status = status, ReservationDate = date };
        db.Reservations.Add(reservation);
        db.SaveChanges();
        return reservation;
    }
}

// A clock that always says the same day, so date rules can be tested.
public sealed class FixedTime(DateOnly today) : TimeProvider
{
    public DateOnly Today { get; } = today;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => new(Today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
}
