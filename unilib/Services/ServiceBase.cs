using Microsoft.EntityFrameworkCore;
using unilib.Data;

namespace unilib.Services;

// Every operation opens its own short-lived LibraryDbContext.
// Errors are rethrown as InvalidOperationException with a message a form can show.
public abstract class ServiceBase
{
    private readonly Func<LibraryDbContext> _createDb;

    protected ServiceBase(Func<LibraryDbContext>? createDb)
    {
        _createDb = createDb ?? (() => new LibraryDbContext());
    }

    protected LibraryDbContext CreateDb() => _createDb();

    protected async Task<T> AddEntityAsync<T>(T entity) where T : class
    {
        using var db = CreateDb();
        db.Add(entity);
        await SaveAsync(db, typeof(T).Name);
        return entity;
    }

    // Copies the scalar values of `entity` onto the stored row with the given key.
    protected async Task UpdateEntityAsync<T>(T entity, params object[] key) where T : class
    {
        using var db = CreateDb();
        var existing = await db.Set<T>().FindAsync(key)
            ?? throw NotFound<T>(key);
        db.Entry(existing).CurrentValues.SetValues(entity);
        await SaveAsync(db, typeof(T).Name);
    }

    protected async Task DeleteEntityAsync<T>(params object[] key) where T : class
    {
        using var db = CreateDb();
        var existing = await db.Set<T>().FindAsync(key)
            ?? throw NotFound<T>(key);
        db.Remove(existing);
        await SaveAsync(db, typeof(T).Name);
    }

    protected static async Task SaveAsync(LibraryDbContext db, string what)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(
                $"Could not save {what}: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
    }

    protected static InvalidOperationException NotFound<T>(params object[] key) =>
        new($"{typeof(T).Name} '{string.Join(", ", key)}' was not found.");
}
