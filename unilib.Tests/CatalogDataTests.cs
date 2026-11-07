using unilib.Models;
using unilib.Services;
using unilib.UI;

namespace unilib.Tests;

public class CatalogDataTests : IDisposable
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private readonly TestDb _db = new();
    private readonly int _userId;
    private readonly int _otherId;

    public CatalogDataTests()
    {
        _db.AddBook("111", amount: 2);
        _db.AddBook("222", amount: 1);
        _userId = _db.AddUser();
        _otherId = _db.AddUser("olena@uni.edu");
    }

    public void Dispose() => _db.Dispose();

    private Task<CatalogData> Load() => CatalogData.LoadAsync(_userId, _db.Create);

    private static Book Book(CatalogData data, string isbn) => data.Books.Single(b => b.Isbn == isbn);

    [Fact]
    public async Task Available_SubtractsEveryonesOpenLoans()
    {
        _db.AddLoan(_otherId, "111", Today.AddDays(-3), Today.AddDays(11));
        _db.AddLoan(_userId, "222", Today.AddDays(-3), Today.AddDays(11));
        _db.AddLoan(_userId, "111", Today.AddDays(-30), Today.AddDays(-16), returned: Today.AddDays(-20));

        var data = await Load();

        Assert.Equal(1, data.Available(Book(data, "111")));
        Assert.Equal("1 available", data.AvailabilityLabel(Book(data, "111")));
        Assert.Equal(0, data.Available(Book(data, "222")));
        Assert.Equal("All copies on loan", data.AvailabilityLabel(Book(data, "222")));
        Assert.Equal("222", Assert.Single(data.MyActiveLoans).BookIsbn);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending, true)]
    [InlineData(ReservationStatus.Active, true)]
    [InlineData(ReservationStatus.Cancelled, false)]
    [InlineData(ReservationStatus.Completed, false)]
    public async Task IsHeld_OnlyForPendingOrApprovedRequests(string status, bool held)
    {
        _db.AddReservation(_userId, "111", status, Today);

        Assert.Equal(held, (await Load()).IsHeld("111"));
    }

    [Fact]
    public async Task Reserve_CreatesPendingRequest()
    {
        var data = await Load();

        var message = await data.ReserveAsync(_userId, Book(data, "111"));

        Assert.StartsWith("Book reserved!", message);
        Assert.True(data.IsHeld("111"));
        Assert.True((await Load()).IsHeld("111"));
    }

    [Fact]
    public async Task Reserve_AlreadyHeld_DoesNotCreateAnother()
    {
        _db.AddReservation(_userId, "111", ReservationStatus.Pending, Today);
        var data = await Load();

        Assert.Equal("You have already reserved this book.", await data.ReserveAsync(_userId, Book(data, "111")));
        Assert.Single((await Load()).MyReservations);
    }

    [Fact]
    public async Task Reserve_AtOpenRequestLimit_IsRefused()
    {
        for (var i = 0; i < LibraryRules.MaxOpenReservations; i++)
        {
            _db.AddBook($"B{i}");
            _db.AddReservation(_userId, $"B{i}", ReservationStatus.Pending, Today);
        }
        var data = await Load();

        Assert.StartsWith("Reservation limit reached", await data.ReserveAsync(_userId, Book(data, "111")));
        Assert.False((await Load()).IsHeld("111"));
    }

    [Fact]
    public async Task ToggleSaved_SavesThenUnsaves()
    {
        var data = await Load();

        Assert.True(await data.ToggleSavedAsync(_userId, Book(data, "111")));
        Assert.True((await Load()).IsSaved("111"));

        Assert.False(await data.ToggleSavedAsync(_userId, Book(data, "111")));
        Assert.False((await Load()).IsSaved("111"));
    }

    [Fact]
    public async Task Location_UsesLibraryAndCategoryShelf()
    {
        var library = _db.AddLibrary("Central");
        _db.AddBook("333", libraryId: library.Id);

        var data = await Load();

        Assert.Equal("Central · Shelf A1", data.Location(Book(data, "333")));
        Assert.Equal("Location not assigned", data.Location(Book(data, "111")));
        Assert.Null(data.Shelf(Book(data, "111")));
    }

    [Fact]
    public async Task Authors_NoAuthors_SaysUnknown()
    {
        Assert.Equal("Unknown author", CatalogData.Authors(Book(await Load(), "111")));
    }
}

public class ShelvingTests
{
    private static List<Category> Categories(int count) =>
        Enumerable.Range(1, count).Select(id => new Category { Id = id * 10, Name = $"C{id}" }).ToList();

    [Fact]
    public void ShelfFor_FollowsCategoryIdOrder()
    {
        var categories = Categories(3);
        categories.Reverse();

        Assert.Equal("A1", Shelving.ShelfFor(10, categories));
        Assert.Equal("A3", Shelving.ShelfFor(30, categories));
    }

    [Fact]
    public void ShelfFor_WrapsAfterLastShelf()
    {
        var categories = Categories(Shelving.Codes.Length + 2);

        Assert.Equal("B6", Shelving.ShelfFor(Shelving.Codes.Length * 10, categories));
        Assert.Equal("A1", Shelving.ShelfFor((Shelving.Codes.Length + 1) * 10, categories));
    }

    [Fact]
    public void ShelfFor_UnknownCategory_UsesFirstShelf()
    {
        Assert.Equal("A1", Shelving.ShelfFor(999, Categories(3)));
    }
}
