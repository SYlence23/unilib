using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using unilib.Data;

namespace unilib.Services;

// Every operation opens its own short-lived LibraryDbContext.
// Refused operations throw ServiceException with a message a form can show; failed saves are
// logged and rethrown as ServiceException without the database details.
// Changes are logged with the id of the signed-in user who made them.
public abstract class ServiceBase
{
    private readonly Func<LibraryDbContext> _createDb;

    // logger: tests pass one to check what is logged; the app uses AppLog.
    protected ServiceBase(Func<LibraryDbContext>? createDb, ILogger? logger = null)
    {
        _createDb = createDb ?? (() => new LibraryDbContext());
        Logger = logger ?? AppLog.CreateLogger(GetType());
    }

    protected ILogger Logger { get; }

    // The signed-in user, or null for start-up tasks and tests.
    protected static int? ActorId => Session.CurrentUser?.Id;

    protected LibraryDbContext CreateDb() => _createDb();

    protected async Task<T> AddEntityAsync<T>(T entity) where T : class
    {
        using var db = CreateDb();
        db.Add(entity);
        await SaveAsync(db, typeof(T).Name);
        Logger.LogInformation("Created {Entity} {Key} by user {ActorId}", typeof(T).Name, KeyOf(db, entity), ActorId);
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
        Logger.LogInformation("Updated {Entity} {Key} by user {ActorId}", typeof(T).Name, string.Join(", ", key), ActorId);
    }

    protected async Task DeleteEntityAsync<T>(params object[] key) where T : class
    {
        using var db = CreateDb();
        var existing = await db.Set<T>().FindAsync(key)
            ?? throw NotFound<T>(key);
        db.Remove(existing);
        await SaveAsync(db, typeof(T).Name);
        Logger.LogInformation("Deleted {Entity} {Key} by user {ActorId}", typeof(T).Name, string.Join(", ", key), ActorId);
    }

    protected async Task SaveAsync(LibraryDbContext db, string what)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // The database error names tables and constraints, so it goes to the log, not to the user.
            Logger.LogError(ex, "Could not save {What}", what);
            throw new ServiceException(
                $"Could not save the {what.ToLowerInvariant()}. Check the entered data and try again.", ex);
        }
    }

    protected static ServiceException NotFound<T>(params object[] key) =>
        new($"{typeof(T).Name} '{string.Join(", ", key)}' was not found.");

    private static string KeyOf<T>(LibraryDbContext db, T entity) where T : class
    {
        var entry = db.Entry(entity);
        var key = entry.Metadata.FindPrimaryKey();
        return key == null ? "" : string.Join(", ", key.Properties.Select(p => entry.Property(p.Name).CurrentValue));
    }
}
