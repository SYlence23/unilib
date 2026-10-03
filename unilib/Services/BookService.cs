using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

public class BookService : ServiceBase
{
    public BookService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<List<Book>> GetAllAsync()
    {
        using var db = CreateDb();
        return await WithDetails(db.Books.AsNoTracking())
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    // Book with category, library, faculty and authors, or null if not found.
    public async Task<Book?> GetByIsbnAsync(string isbn)
    {
        using var db = CreateDb();
        return await WithDetails(db.Books.AsNoTracking())
            .SingleOrDefaultAsync(b => b.Isbn == isbn);
    }

    // Case-insensitive match on title, ISBN or author name.
    public async Task<List<Book>> SearchAsync(string text)
    {
        var term = text.Trim().ToLower();

        using var db = CreateDb();
        return await WithDetails(db.Books.AsNoTracking())
            .Where(b => b.Title.ToLower().Contains(term)
                || b.Isbn.ToLower().Contains(term)
                || b.BookAuthors.Any(ba => ba.Author.FirstName.ToLower().Contains(term)
                    || ba.Author.LastName.ToLower().Contains(term)))
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    // Copies not currently on loan: Amount minus loans without a return date.
    public async Task<int> GetAvailableCopiesAsync(string isbn)
    {
        using var db = CreateDb();
        var amount = await db.Books.Where(b => b.Isbn == isbn).Select(b => b.Amount).SingleOrDefaultAsync();
        var onLoan = await db.UserBooks.CountAsync(ub => ub.BookIsbn == isbn && ub.ReturnDate == null);
        return Math.Max((amount ?? 0) - onLoan, 0);
    }

    public Task<Book> CreateAsync(Book book) => AddEntityAsync(book);

    public Task UpdateAsync(Book book) => UpdateEntityAsync(book, book.Isbn);

    public Task DeleteAsync(string isbn) => DeleteEntityAsync<Book>(isbn);

    public Task AddAuthorAsync(string isbn, int authorId) =>
        AddEntityAsync(new BookAuthor { BookId = isbn, AuthorId = authorId });

    public Task RemoveAuthorAsync(string isbn, int authorId) =>
        DeleteEntityAsync<BookAuthor>(isbn, authorId);

    private static IQueryable<Book> WithDetails(IQueryable<Book> books) =>
        books
            .Include(b => b.Category)
            .Include(b => b.Library)
            .Include(b => b.Faculty)
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author);
}
