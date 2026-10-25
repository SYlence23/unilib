using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

public class LibraryService : ServiceBase
{
    public LibraryService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<List<Library>> GetAllAsync()
    {
        using var db = CreateDb();
        return await db.Libraries.AsNoTracking().OrderBy(l => l.Name).ToListAsync();
    }

    public async Task<Library?> GetByIdAsync(int id)
    {
        using var db = CreateDb();
        return await db.Libraries.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Library> CreateAsync(Library library)
    {
        ValidateCoordinates(library);
        return await AddEntityAsync(library);
    }

    public async Task UpdateAsync(Library library)
    {
        ValidateCoordinates(library);
        await UpdateEntityAsync(library, library.Id);
    }

    public Task DeleteAsync(int id) => DeleteEntityAsync<Library>(id);

    // Same rules as the check constraints on the library table.
    private static void ValidateCoordinates(Library library)
    {
        if ((library.Latitude is null) != (library.Longitude is null))
            throw new InvalidOperationException("Latitude and longitude must both be set or both be empty.");

        if (library.Latitude is < -90 or > 90)
            throw new InvalidOperationException($"Latitude must be between -90 and 90, got {library.Latitude}.");

        if (library.Longitude is < -180 or > 180)
            throw new InvalidOperationException($"Longitude must be between -180 and 180, got {library.Longitude}.");
    }
}
