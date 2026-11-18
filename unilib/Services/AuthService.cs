using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using unilib.Data;
using unilib.Models;

namespace unilib.Services;

// Registration and login. Passwords are hashed with ASP.NET Core Identity's
// PasswordHasher (salted PBKDF2); plain passwords are never stored or logged.
// Sign-ins, failed sign-ins and password changes are logged for auditing,
// by user id. Email addresses are personal data and are never logged.
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
        firstName = Validation.Required(firstName, "First name", Validation.NameLength);
        lastName = Validation.Required(lastName, "Last name", Validation.NameLength);
        email = Validation.Email(email);
        studentId = Validation.Optional(studentId, "Student ID", Validation.StudentIdLength);
        Validation.Password(password);

        using var db = CreateDb();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            Logger.LogWarning("Registration refused: the email is already registered");
            throw new ServiceException($"A user with email '{email}' already exists.");
        }

        var studentRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == RoleNames.Student)
            ?? throw new ServiceException($"Role '{RoleNames.Student}' is missing in the database.");

        var user = new User
        {
            RoleId = studentRole.Id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            StudentId = studentId,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, password);

        db.Users.Add(user);
        await SaveAsync(db, nameof(User));
        Logger.LogInformation("Registered user {UserId} as {Role}", user.Id, RoleNames.Student);
        return user;
    }

    // Returns the user with Role loaded, or null if the email or password is wrong.
    public async Task<User?> LoginAsync(string email, string password)
    {
        email = NormalizeEmail(email ?? "");
        // Input no account could have: refuse it before touching the database or the hasher.
        if (email.Length == 0 || email.Length > Validation.EmailLength
            || string.IsNullOrEmpty(password) || password.Length > Validation.MaxPasswordLength)
        {
            Logger.LogWarning("Failed sign-in: invalid input");
            return null;
        }

        using var db = CreateDb();
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            Logger.LogWarning("Failed sign-in: no such account");
            return null;
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            Logger.LogWarning("Failed sign-in for user {UserId}: wrong password", user.Id);
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
        Validation.Password(newPassword);
        if (newPassword == DefaultAdminPassword)
            throw new ServiceException("This password is not allowed.");
        if (newPassword == oldPassword)
            throw new ServiceException("Choose a password different from the current one.");

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
            Logger.LogWarning("No admin account exists, and user {UserId} holds the admin email with their own password",
                admin.Id);
            return;
        }

        admin.RoleId = adminRole.Id;
        await SaveAsync(db, nameof(User));
        Logger.LogWarning("Made user {UserId} the admin with the default password; it must be changed at first sign-in",
            admin.Id);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
