using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

public class RoleService : ServiceBase
{
    public RoleService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<List<Role>> GetAllAsync()
    {
        using var db = CreateDb();
        return await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<Role?> GetByIdAsync(int id)
    {
        using var db = CreateDb();
        return await db.Roles.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id);
    }

    public Task<Role> CreateAsync(Role role) => AddEntityAsync(role);

    public Task UpdateAsync(Role role) => UpdateEntityAsync(role, role.Id);

    public Task DeleteAsync(int id) => DeleteEntityAsync<Role>(id);
}
