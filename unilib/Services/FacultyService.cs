using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

public class FacultyService : ServiceBase
{
    public FacultyService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<List<Faculty>> GetAllAsync()
    {
        using var db = CreateDb();
        return await db.Faculties.AsNoTracking().OrderBy(f => f.Name).ToListAsync();
    }

    public async Task<Faculty?> GetByIdAsync(int id)
    {
        using var db = CreateDb();
        return await db.Faculties.AsNoTracking().SingleOrDefaultAsync(f => f.Id == id);
    }

    public Task<Faculty> CreateAsync(Faculty faculty) => AddEntityAsync(faculty);

    public Task UpdateAsync(Faculty faculty) => UpdateEntityAsync(faculty, faculty.Id);

    public Task DeleteAsync(int id) => DeleteEntityAsync<Faculty>(id);
}
