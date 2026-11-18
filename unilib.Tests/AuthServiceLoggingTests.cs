using Microsoft.Extensions.Logging;
using unilib.Services;

namespace unilib.Tests;

public class AuthServiceLoggingTests : IDisposable
{
    private const string Email = "ivan.petrenko@uni.edu";
    private const string Password = "Secret123!";

    private readonly TestDb _db = new();
    private readonly ListLogger<AuthService> _log = new();
    private readonly AuthService _auth;

    public AuthServiceLoggingTests() => _auth = new AuthService(_db.Create, _log);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Register_LogsUserIdWithoutPasswordOrFullEmail()
    {
        var user = await _auth.RegisterAsync("Ivan", "Petrenko", Email, Password);

        var entry = Assert.Single(_log.Entries, e => e.Message.StartsWith("Registered user"));
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains($"user {user.Id}", entry.Message);
        AssertNoSecrets();
    }

    [Fact]
    public async Task Login_Success_IsLoggedAsInformation()
    {
        var user = await _auth.RegisterAsync("Ivan", "Petrenko", Email, Password);

        await _auth.LoginAsync(Email, Password);

        var entry = Assert.Single(_log.Entries, e => e.Message.Contains("signed in"));
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains($"User {user.Id} signed in as student", entry.Message);
        AssertNoSecrets();
    }

    [Fact]
    public async Task Login_WrongPassword_IsLoggedAsWarning()
    {
        await _auth.RegisterAsync("Ivan", "Petrenko", Email, Password);

        await _auth.LoginAsync(Email, "Wrong-password-1");

        var entry = Assert.Single(_log.Entries, e => e.Message.StartsWith("Failed sign-in"));
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("wrong password", entry.Message);
        Assert.DoesNotContain("Wrong-password-1", _log.AllText);
        AssertNoSecrets();
    }

    [Fact]
    public async Task Login_UnknownEmail_IsLoggedAsWarningWithMaskedEmail()
    {
        await _auth.LoginAsync(Email, Password);

        var entry = Assert.Single(_log.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("iv***@uni.edu", entry.Message);
        AssertNoSecrets();
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_IsLoggedAsWarning()
    {
        var user = await _auth.RegisterAsync("Ivan", "Petrenko", Email, Password);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _auth.ChangePasswordAsync(user.Id, "Not-my-password", "NewSecret1!"));

        Assert.Contains(_log.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("wrong current password"));
        Assert.DoesNotContain("NewSecret1!", _log.AllText);
    }

    [Fact]
    public async Task EnsureAdmin_DoesNotLogASignIn()
    {
        await _auth.EnsureAdminAsync("admin@unilib.com");

        Assert.DoesNotContain(_log.Entries, e => e.Message.Contains("signed in"));
        Assert.Contains(_log.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("default password"));
        Assert.DoesNotContain(AuthService.DefaultAdminPassword, _log.AllText);
    }

    private void AssertNoSecrets()
    {
        Assert.DoesNotContain(Password, _log.AllText);
        Assert.DoesNotContain(Email, _log.AllText);
    }

    [Theory]
    [InlineData("ivan.petrenko@uni.edu", "iv***@uni.edu")]
    [InlineData("a@uni.edu", "a***@uni.edu")]
    [InlineData("no-at-sign", "no***")]
    [InlineData("", "(empty)")]
    [InlineData(null, "(empty)")]
    public void MaskEmail_HidesTheName(string? email, string expected) =>
        Assert.Equal(expected, AppLog.MaskEmail(email));
}
