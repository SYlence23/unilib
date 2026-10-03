using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

public class CategoryService : ServiceBase
{
    public CategoryService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<List<Category>> GetAllAsync()
    {
        using var db = CreateDb();
        return await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        using var db = CreateDb();
        return await db.Categories.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id);
    }

    public Task<Category> CreateAsync(Category category) => AddEntityAsync(category);

    public Task UpdateAsync(Category category) => UpdateEntityAsync(category, category.Id);

    public Task DeleteAsync(int id) => DeleteEntityAsync<Category>(id);
}
