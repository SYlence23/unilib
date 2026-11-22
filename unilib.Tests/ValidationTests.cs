using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class ValidationTests : IDisposable
{
    private readonly TestDb _db = new();

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData("  Ivan  ", "Ivan")]
    [InlineData("Ірина", "Ірина")]
    public void Required_TrimsValue(string input, string expected) =>
        Assert.Equal(expected, Validation.Required(input, "Name", 50));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Required_RejectsBlank(string? input)
    {
        var ex = Assert.Throws<ServiceException>(() => Validation.Required(input, "First name", 50));
        Assert.Equal("First name is required.", ex.Message);
    }

    [Fact]
    public void Required_RejectsTooLong()
    {
        var ex = Assert.Throws<ServiceException>(() => Validation.Required(new string('a', 51), "First name", 50));
        Assert.Equal("First name must be at most 50 characters.", ex.Message);
    }

    [Fact]
    public void Optional_TurnsBlankIntoNull() => Assert.Null(Validation.Optional("  ", "Student ID", 20));

    [Theory]
    [InlineData(" Ivan@Uni.EDU ", "ivan@uni.edu")]
    [InlineData("o.petrenko+lib@lnu.edu.ua", "o.petrenko+lib@lnu.edu.ua")]
    public void Email_AcceptsAndNormalizes(string input, string expected) =>
        Assert.Equal(expected, Validation.Email(input));

    [Theory]
    [InlineData("ivan")]
    [InlineData("ivan@uni")]
    [InlineData("iv an@uni.edu")]
    [InlineData("@uni.edu")]
    [InlineData("ivan@@uni.edu")]
    public void Email_RejectsInvalid(string input) =>
        Assert.Throws<ServiceException>(() => Validation.Email(input));

    [Fact]
    public void Email_RejectsTooLong() =>
        Assert.Throws<ServiceException>(() => Validation.Email(new string('a', 95) + "@uni.edu"));

    [Theory]
    [InlineData(null)]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Password_RejectsShort(string? password) =>
        Assert.Throws<ServiceException>(() => Validation.Password(password));

    [Fact]
    public void Password_RejectsTooLong() =>
        Assert.Throws<ServiceException>(() => Validation.Password(new string('a', Validation.MaxPasswordLength + 1)));

    [Fact]
    public void Password_AcceptsMinimumLength() => Validation.Password("12345678");

    [Theory]
    [InlineData("+380 (32) 239-41-11")]
    [InlineData("0322394111")]
    public void Phone_AcceptsDigitsAndSeparators(string phone) => Assert.Equal(phone, Validation.Phone(phone));

    [Theory]
    [InlineData("call me")]
    [InlineData("<script>")]
    public void Phone_RejectsOtherCharacters(string phone) =>
        Assert.Throws<ServiceException>(() => Validation.Phone(phone));

    [Theory]
    [InlineData("978-0-13-468599-1", "978-0-13-468599-1")]
    [InlineData("080442957x", "080442957X")]
    public void Isbn_AcceptsDigitsXAndHyphens(string input, string expected) =>
        Assert.Equal(expected, Validation.Isbn(input));

    [Theory]
    [InlineData("978'; DROP TABLE book;--")]
    [InlineData("abc")]
    public void Isbn_RejectsOtherCharacters(string isbn) =>
        Assert.Throws<ServiceException>(() => Validation.Isbn(isbn));

    [Theory]
    [InlineData("", "Petrenko", "ivan@uni.edu", "Secret123!")]
    [InlineData("Ivan", "  ", "ivan@uni.edu", "Secret123!")]
    [InlineData("Ivan", "Petrenko", "not-an-email", "Secret123!")]
    [InlineData("Ivan", "Petrenko", "ivan@uni.edu", "short")]
    public async Task Register_InvalidInput_IsRefusedAndNothingIsStored(
        string first, string last, string email, string password)
    {
        await Assert.ThrowsAsync<ServiceException>(
            () => new AuthService(_db.Create).RegisterAsync(first, last, email, password));

        using var db = _db.Create();
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Register_TrimsNamesAndDropsBlankStudentId()
    {
        var user = await new AuthService(_db.Create).RegisterAsync("  Ivan ", " Petrenko ", "ivan@uni.edu", "Secret123!", "  ");

        Assert.Equal("Ivan", user.FirstName);
        Assert.Equal("Petrenko", user.LastName);
        Assert.Null(user.StudentId);
    }

    [Theory]
    [InlineData("short1")]
    [InlineData(AuthService.DefaultAdminPassword)]
    [InlineData("Secret123!")]
    public async Task ChangePassword_WeakDefaultOrSamePassword_IsRefused(string newPassword)
    {
        var auth = new AuthService(_db.Create);
        var user = await auth.RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!");

        await Assert.ThrowsAsync<ServiceException>(() => auth.ChangePasswordAsync(user.Id, "Secret123!", newPassword));

        Assert.NotNull(await auth.LoginAsync("ivan@uni.edu", "Secret123!"));
    }

    [Theory]
    [InlineData("", "Secret123!")]
    [InlineData("ivan@uni.edu", "")]
    public async Task Login_BlankInput_ReturnsNull(string email, string password)
    {
        var auth = new AuthService(_db.Create);
        await auth.RegisterAsync("Ivan", "Petrenko", "ivan@uni.edu", "Secret123!");

        Assert.Null(await auth.LoginAsync(email, password));
    }

    [Fact]
    public async Task Login_HugePassword_ReturnsNullWithoutHashing() =>
        Assert.Null(await new AuthService(_db.Create).LoginAsync("ivan@uni.edu", new string('a', 1_000_000)));

    [Fact]
    public async Task UpdateUser_InvalidPhone_IsRefused()
    {
        var users = new UserService(_db.Create);
        var user = (await users.GetByIdAsync(_db.AddUser()))!;
        user.Phone = "call me";

        await Assert.ThrowsAsync<ServiceException>(() => users.UpdateAsync(user));
    }

    [Fact]
    public async Task AddBook_WithoutAuthors_IsRefused() =>
        await Assert.ThrowsAsync<ServiceException>(() => new BookService(_db.Create)
            .AddWithAuthorsAsync(new Book { Isbn = "9780134685991", Title = "Effective Java" }, "Programming",
                Array.Empty<(string, string)>()));

    [Fact]
    public async Task AddBook_TooLongTitle_IsRefused() =>
        await Assert.ThrowsAsync<ServiceException>(() => new BookService(_db.Create)
            .AddWithAuthorsAsync(new Book { Isbn = "9780134685991", Title = new string('a', 201) }, "Programming",
                new[] { ("Joshua", "Bloch") }));

    [Fact]
    public async Task AddBook_NegativeCopies_IsRefused() =>
        await Assert.ThrowsAsync<ServiceException>(() => new BookService(_db.Create)
            .AddWithAuthorsAsync(new Book { Isbn = "9780134685991", Title = "Effective Java", Amount = -1 },
                "Programming", new[] { ("Joshua", "Bloch") }));

    [Fact]
    public async Task CreateLibrary_BlankName_IsRefused() =>
        await Assert.ThrowsAsync<ServiceException>(() => new LibraryService(_db.Create)
            .CreateAsync(new Library { Name = " " }));
}
