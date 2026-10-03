using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

public class FavoriteService : ServiceBase
{
    public FavoriteService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<List<Favorite>> GetByUserAsync(int userId)
    {
        using var db = CreateDb();
        return await db.Favorites.AsNoTracking()
            .Include(f => f.Book)
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.AddedAt)
            .ToListAsync();
    }

    public async Task<bool> IsFavoriteAsync(int userId, string isbn)
    {
        using var db = CreateDb();
        return await db.Favorites.AnyAsync(f => f.UserId == userId && f.BookId == isbn);
    }

    public Task<Favorite> AddAsync(int userId, string isbn) =>
        AddEntityAsync(new Favorite
        {
            UserId = userId,
            BookId = isbn,
            AddedAt = DateOnly.FromDateTime(DateTime.Today)
        });

    public Task RemoveAsync(int userId, string isbn) => DeleteEntityAsync<Favorite>(userId, isbn);
}
