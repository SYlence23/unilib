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

    public Task<Library> CreateAsync(Library library) => AddEntityAsync(library);

    public Task UpdateAsync(Library library) => UpdateEntityAsync(library, library.Id);

    public Task DeleteAsync(int id) => DeleteEntityAsync<Library>(id);
}
