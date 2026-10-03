using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

public class AuthorService : ServiceBase
{
    public AuthorService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<List<Author>> GetAllAsync()
    {
        using var db = CreateDb();
        return await db.Authors.AsNoTracking()
            .OrderBy(a => a.LastName).ThenBy(a => a.FirstName)
            .ToListAsync();
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        using var db = CreateDb();
        return await db.Authors.AsNoTracking()
            .Include(a => a.User)
            .SingleOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Book>> GetBooksAsync(int authorId)
    {
        using var db = CreateDb();
        return await db.BookAuthors.AsNoTracking()
            .Where(ba => ba.AuthorId == authorId)
            .Select(ba => ba.Book)
            .OrderBy(b => b.Title)
            .ToListAsync();
    }

    public Task<Author> CreateAsync(Author author) => AddEntityAsync(author);

    public Task UpdateAsync(Author author) => UpdateEntityAsync(author, author.Id);

    public Task DeleteAsync(int id) => DeleteEntityAsync<Author>(id);
}
