using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

// New users are created through AuthService.RegisterAsync.
public class UserService : ServiceBase
{
    public UserService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<List<User>> GetAllAsync()
    {
        using var db = CreateDb();
        return await WithDetails(db.Users.AsNoTracking())
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var db = CreateDb();
        return await WithDetails(db.Users.AsNoTracking()).SingleOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByStudentIdAsync(string studentId)
    {
        using var db = CreateDb();
        return await WithDetails(db.Users.AsNoTracking()).SingleOrDefaultAsync(u => u.StudentId == studentId);
    }

    // Updates profile fields and role. Password and CreatedAt are never changed here.
    public async Task UpdateAsync(User user)
    {
        using var db = CreateDb();
        var existing = await db.Users.FindAsync(user.Id) ?? throw NotFound<User>(user.Id);
        var oldRoleId = existing.RoleId;

        existing.RoleId = user.RoleId;
        existing.FacultyId = user.FacultyId;
        existing.LibraryId = user.LibraryId;
        existing.FirstName = Validation.Required(user.FirstName, "First name", Validation.NameLength);
        existing.LastName = Validation.Required(user.LastName, "Last name", Validation.NameLength);
        existing.StudentId = Validation.Optional(user.StudentId, "Student ID", Validation.StudentIdLength);
        existing.Email = Validation.Email(user.Email);
        existing.Phone = Validation.Phone(user.Phone);

        await SaveAsync(db, nameof(User));
        Logger.LogInformation("Updated user {UserId}, by user {ActorId}", user.Id, ActorId);
        if (oldRoleId != user.RoleId)
            Logger.LogWarning("Role of user {UserId} changed from {OldRoleId} to {NewRoleId}, by user {ActorId}",
                user.Id, oldRoleId, user.RoleId, ActorId);
    }

    public Task DeleteAsync(int id) => DeleteEntityAsync<User>(id);

    private static IQueryable<User> WithDetails(IQueryable<User> users) =>
        users
            .Include(u => u.Role)
            .Include(u => u.Faculty)
            .Include(u => u.Library);
}
