using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly AuthService _auth;

    public AuthServiceTests() => _auth = new AuthService(_db.Create);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Register_CreatesStudentWithHashedPassword()
    {
        var user = await _auth.RegisterAsync("Ivan", "Petrenko", "Ivan@Uni.edu", "Secret123!", "ST-001");

        using var db = _db.Create();
        var saved = db.Users.Single(u => u.Id == user.Id);
        Assert.Equal("ivan@uni.edu", saved.Email);
        Assert.NotEqual("Secret123!", saved.PasswordHash);
        Assert.Equal(RoleNames.Student, db.Roles.Single(r => r.Id == saved.RoleId).Name);
        Assert.Equal(DateTimeKind.Utc, user.CreatedAt.Kind);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Throws()
    {
        await _auth.RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _auth.RegisterAsync("Other", "Person", "IVAN@uni.edu", "Another1!"));
    }

    [Fact]
    public async Task Login_CorrectPassword_ReturnsUserWithRole()
    {
        await _auth.RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!");

        var user = await _auth.LoginAsync(" Ivan@uni.edu ", "Secret123!");

        Assert.NotNull(user);
        Assert.Equal(RoleNames.Student, user.Role.Name);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsNull()
    {
        await _auth.RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!");

        Assert.Null(await _auth.LoginAsync("ivan@uni.edu", "wrong"));
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsNull()
    {
        Assert.Null(await _auth.LoginAsync("nobody@uni.edu", "Secret123!"));
    }

    [Fact]
    public async Task ChangePassword_ReplacesPassword()
    {
        var user = await _auth.RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!");

        await _auth.ChangePasswordAsync(user.Id, "Secret123!", "NewSecret1!");

        Assert.Null(await _auth.LoginAsync("ivan@uni.edu", "Secret123!"));
        Assert.NotNull(await _auth.LoginAsync("ivan@uni.edu", "NewSecret1!"));
    }

    [Fact]
    public async Task ChangePassword_WrongOldPassword_Throws()
    {
        var user = await _auth.RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _auth.ChangePasswordAsync(user.Id, "wrong", "NewSecret1!"));
    }

    private const string AdminEmail = "admin@unilib.com";

    private string RoleOf(string email)
    {
        using var db = _db.Create();
        var user = db.Users.Single(u => u.Email == email);
        return db.Roles.Single(r => r.Id == user.RoleId).Name;
    }

    private void MakeAdmin(string email)
    {
        using var db = _db.Create();
        db.Users.Single(u => u.Email == email).RoleId = db.Roles.Single(r => r.Name == RoleNames.Admin).Id;
        db.SaveChanges();
    }

    [Fact]
    public async Task EnsureAdmin_EmptyDatabase_CreatesAdminWithDefaultPassword()
    {
        await _auth.EnsureAdminAsync(AdminEmail);

        Assert.Equal(RoleNames.Admin, RoleOf(AdminEmail));
        Assert.NotNull(await _auth.LoginAsync(AdminEmail, AuthService.DefaultAdminPassword));
    }

    [Fact]
    public async Task EnsureAdmin_SeededAccountCreatedAsStudent_IsPromoted()
    {
        await _auth.RegisterAsync("Admin", "User", AdminEmail, AuthService.DefaultAdminPassword);

        await _auth.EnsureAdminAsync(AdminEmail);

        Assert.Equal(RoleNames.Admin, RoleOf(AdminEmail));
    }

    [Fact]
    public async Task EnsureAdmin_EmailRegisteredWithOwnPassword_IsNotPromoted()
    {
        await _auth.RegisterAsync("Someone", "Else", AdminEmail, "TheirOwn123!");

        await _auth.EnsureAdminAsync(AdminEmail);

        Assert.Equal(RoleNames.Student, RoleOf(AdminEmail));
    }

    [Fact]
    public async Task EnsureAdmin_AdminAlreadyExists_ChangesNothing()
    {
        await _auth.RegisterAsync("Real", "Admin", "boss@uni.edu", "Secret123!");
        MakeAdmin("boss@uni.edu");

        await _auth.EnsureAdminAsync(AdminEmail);

        using var db = _db.Create();
        Assert.False(db.Users.Any(u => u.Email == AdminEmail));
    }
}
