using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

// Registration and login. Passwords are hashed with ASP.NET Core Identity's
// PasswordHasher (salted PBKDF2); plain passwords are never stored.
public class AuthService : ServiceBase
{
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(Func<LibraryDbContext>? createDb = null) : base(createDb)
    {
    }

    public async Task<User> RegisterAsync(
        string firstName, string lastName, string email, string password, string? studentId = null)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Password is required.");

        email = NormalizeEmail(email);

        using var db = CreateDb();
        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException($"A user with email '{email}' already exists.");

        var studentRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == RoleNames.Student)
            ?? throw new InvalidOperationException($"Role '{RoleNames.Student}' is missing in the database.");

        var user = new User
        {
            RoleId = studentRole.Id,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email,
            StudentId = string.IsNullOrWhiteSpace(studentId) ? null : studentId.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, password);

        db.Users.Add(user);
        await SaveAsync(db, nameof(User));
        return user;
    }

    // Returns the user with Role loaded, or null if the email or password is wrong.
    public async Task<User?> LoginAsync(string email, string password)
    {
        email = NormalizeEmail(email);

        using var db = CreateDb();
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            await SaveAsync(db, nameof(User));
        }

        return user;
    }

    public async Task ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new InvalidOperationException("New password is required.");

        using var db = CreateDb();
        var user = await db.Users.FindAsync(userId) ?? throw NotFound<User>(userId);

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword) == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Current password is incorrect.");

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await SaveAsync(db, nameof(User));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
