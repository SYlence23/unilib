using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

// Registration and login. Passwords are hashed with ASP.NET Core Identity's
// PasswordHasher (salted PBKDF2); plain passwords are never stored or logged.
// Sign-ins, failed sign-ins and password changes are logged for auditing,
// with masked email addresses.
public class AuthService : ServiceBase
{
    // Password of the seeded admin account. Signing in with it forces a password change.
    public const string DefaultAdminPassword = "admin123";

    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(Func<LibraryDbContext>? createDb = null, ILogger<AuthService>? logger = null)
        : base(createDb, logger)
    {
    }

    public async Task<User> RegisterAsync(
        string firstName, string lastName, string email, string password, string? studentId = null)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ServiceException("Password is required.");

        email = NormalizeEmail(email);

        using var db = CreateDb();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            Logger.LogWarning("Registration refused: {Email} is already registered", AppLog.MaskEmail(email));
            throw new ServiceException($"A user with email '{email}' already exists.");
        }

        var studentRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == RoleNames.Student)
            ?? throw new ServiceException($"Role '{RoleNames.Student}' is missing in the database.");

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
        Logger.LogInformation("Registered user {UserId} ({Email}) as {Role}",
            user.Id, AppLog.MaskEmail(email), RoleNames.Student);
        return user;
    }

    // Returns the user with Role loaded, or null if the email or password is wrong.
    public async Task<User?> LoginAsync(string email, string password)
    {
        email = NormalizeEmail(email);

        using var db = CreateDb();
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            Logger.LogWarning("Failed sign-in for {Email}: no such account", AppLog.MaskEmail(email));
            return null;
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            Logger.LogWarning("Failed sign-in for user {UserId} ({Email}): wrong password",
                user.Id, AppLog.MaskEmail(email));
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            await SaveAsync(db, nameof(User));
            Logger.LogInformation("Rehashed the password of user {UserId} with current settings", user.Id);
        }

        Logger.LogInformation("User {UserId} signed in as {Role}", user.Id, user.Role.Name);
        return user;
    }

    public async Task ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ServiceException("New password is required.");

        using var db = CreateDb();
        var user = await db.Users.FindAsync(userId) ?? throw NotFound<User>(userId);

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, oldPassword) == PasswordVerificationResult.Failed)
        {
            Logger.LogWarning("Password change refused for user {UserId}: wrong current password", userId);
            throw new ServiceException("Current password is incorrect.");
        }

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        await SaveAsync(db, nameof(User));
        Logger.LogInformation("User {UserId} changed their password", userId);
    }

    // Makes sure the database has an admin. Once any admin exists this does nothing.
    // Otherwise it creates the account with DefaultAdminPassword (which must be changed at
    // first login) and gives it the admin role (RegisterAsync always creates students).
    // An existing account with this email is promoted only while it still has the default
    // password: that is the seeded account (older versions created it as a student), not
    // someone who registered the address with their own password.
    public async Task EnsureAdminAsync(string email)
    {
        email = NormalizeEmail(email);
        using var db = CreateDb();
        var adminRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == RoleNames.Admin);
        if (adminRole == null || await db.Users.AnyAsync(u => u.RoleId == adminRole.Id))
            return;

        var admin = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (admin == null)
        {
            await RegisterAsync("Admin", "User", email, DefaultAdminPassword);
            admin = await db.Users.SingleAsync(u => u.Email == email);
        }
        else if (_hasher.VerifyHashedPassword(admin, admin.PasswordHash, DefaultAdminPassword)
            == PasswordVerificationResult.Failed)
        {
            Logger.LogWarning("No admin account exists, and {Email} is taken by a user with their own password",
                AppLog.MaskEmail(email));
            return;
        }

        admin.RoleId = adminRole.Id;
        await SaveAsync(db, nameof(User));
        Logger.LogWarning("Made user {UserId} the admin with the default password; it must be changed at first sign-in",
            admin.Id);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
