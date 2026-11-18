using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

    public Task<Book> CreateAsync(Book book)
    {
        ValidateBook(book);
        return AddEntityAsync(book);
    }

    public const string DuplicateIsbnMessage = "A book with this ISBN is already catalogued.";

    // Adds a book with its authors in a single save, so a failure stores nothing.
    // With newCategory set, the category with that name (ignoring case) is used or created;
    // otherwise book.CategoryId is used. Authors are matched by name ignoring case and created if unknown.
    // Names are compared in C#, not in SQL, because Postgres lower() depends on the database
    // locale and may not fold Cyrillic; OrdinalIgnoreCase behaves the same everywhere.
    public async Task<Book> AddWithAuthorsAsync(Book book, string? newCategory, IEnumerable<(string First, string Last)> authors)
    {
        ValidateBook(book);
        var authorNames = authors
            .Select(a => (First: Validation.Optional(a.First, "Author first name", Validation.NameLength) ?? "",
                Last: Validation.Required(a.Last, "Author last name", Validation.NameLength)))
            .ToList();
        if (authorNames.Count == 0)
            throw new ServiceException("At least one author is required.");

        using var db = CreateDb();
        if (await IsbnExistsAsync(book.Isbn))
            throw new ServiceException(DuplicateIsbnMessage);

        var categoryName = Validation.Optional(newCategory, "Category", Validation.CategoryLength) ?? "";
        if (categoryName.Length > 0)
            book.Category = FindByName(await db.Categories.ToListAsync(), categoryName)
                ?? new Category { Name = categoryName };

        var unique = authorNames.DistinctBy(a => $"{a.First}\n{a.Last}", StringComparer.OrdinalIgnoreCase).ToList();
        if (unique.Count > 0)
        {
            var known = await db.Authors.ToListAsync();
            foreach (var (first, last) in unique)
            {
                var author = known.FirstOrDefault(a =>
                        string.Equals(a.FirstName, first, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(a.LastName, last, StringComparison.OrdinalIgnoreCase))
                    ?? new Author { FirstName = first, LastName = last };
                book.BookAuthors.Add(new BookAuthor { Author = author });
            }
        }

        db.Books.Add(book);
        try
        {
            await SaveAsync(db, nameof(Book));
        }
        catch (ServiceException ex)
        {
            // Someone else saved the same ISBN or category between our checks and the save.
            if (await IsbnExistsAsync(book.Isbn))
                throw new ServiceException(DuplicateIsbnMessage, ex);
            if (categoryName.Length > 0 && book.Category.Id == 0)
            {
                using var check = CreateDb();
                if (FindByName(await check.Categories.ToListAsync(), categoryName) != null)
                    throw new ServiceException(
                        $"The category '{categoryName}' was just added by someone else. Press Add again to use it.", ex);
            }
            throw;
        }
        Logger.LogInformation("Added book {Isbn} with {AuthorCount} authors, by user {ActorId}",
            book.Isbn, unique.Count, ActorId);
        return book;
    }

    private async Task<bool> IsbnExistsAsync(string isbn)
    {
        using var db = CreateDb();
        return await db.Books.AnyAsync(b => b.Isbn == isbn);
    }

    private static Category? FindByName(IEnumerable<Category> categories, string name) =>
        categories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    public Task UpdateAsync(Book book)
    {
        ValidateBook(book);
        return UpdateEntityAsync(book, book.Isbn);
    }

    // Cleans the ISBN and title in place, or throws ServiceException.
    private static void ValidateBook(Book book)
    {
        book.Isbn = Validation.Isbn(book.Isbn);
        book.Title = Validation.Required(book.Title, "Title", Validation.TitleLength);
        book.Language = Validation.Optional(book.Language, "Language", Validation.LanguageLength);
        if (book.Amount < 0)
            throw new ServiceException("Copies must be 0 or more.");
    }

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
