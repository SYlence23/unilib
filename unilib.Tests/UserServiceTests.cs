using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class UserServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly UserService _users;
    private readonly int _userId;

    public UserServiceTests()
    {
        _users = new UserService(_db.Create);
        _userId = _db.AddUser();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Update_ChangesProfileAndNormalizesEmail_KeepsPassword()
    {
        var user = (await _users.GetByIdAsync(_userId))!;
        var hash = user.PasswordHash;
        user.FirstName = "Olena";
        user.Email = "  Olena.New@Uni.EDU ";
        user.StudentId = "LNU-42";

        await _users.UpdateAsync(user);

        var saved = (await _users.GetByIdAsync(_userId))!;
        Assert.Equal("Olena", saved.FirstName);
        Assert.Equal("olena.new@uni.edu", saved.Email);
        Assert.Equal(hash, saved.PasswordHash);
        Assert.Equal(RoleNames.Student, saved.Role.Name);
        Assert.Equal(_userId, (await _users.GetByStudentIdAsync("LNU-42"))!.Id);
    }

    [Fact]
    public async Task Update_EmailOfAnotherUser_Throws()
    {
        _db.AddUser("taken@uni.edu");
        var user = (await _users.GetByIdAsync(_userId))!;
        user.Email = "taken@uni.edu";

        await Assert.ThrowsAsync<InvalidOperationException>(() => _users.UpdateAsync(user));
    }

    [Fact]
    public async Task Update_UnknownUser_Throws()
    {
        var user = (await _users.GetByIdAsync(_userId))!;
        user.Id = 9999;

        await Assert.ThrowsAsync<InvalidOperationException>(() => _users.UpdateAsync(user));
    }

    [Fact]
    public async Task GetAll_SortsByLastName()
    {
        await new AuthService(_db.Create).RegisterAsync("Anna", "Antonenko", "anna@uni.edu", "Secret123!");

        Assert.Equal(new[] { "Antonenko", "Petrenko" }, (await _users.GetAllAsync()).Select(u => u.LastName));
    }
}
