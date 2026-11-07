using unilib.Services;

namespace unilib.Tests;

public class FavoriteServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FavoriteService _favorites;
    private readonly int _userId;

    public FavoriteServiceTests()
    {
        _favorites = new FavoriteService(_db.Create);
        _db.AddBook("111", title: "Clean Code");
        _userId = _db.AddUser();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Add_ThenRemove_UpdatesSavedBooks()
    {
        await _favorites.AddAsync(_userId, "111");

        Assert.True(await _favorites.IsFavoriteAsync(_userId, "111"));
        Assert.Equal("Clean Code", Assert.Single(await _favorites.GetByUserAsync(_userId)).Book.Title);

        await _favorites.RemoveAsync(_userId, "111");

        Assert.False(await _favorites.IsFavoriteAsync(_userId, "111"));
        Assert.Empty(await _favorites.GetByUserAsync(_userId));
    }

    [Fact]
    public async Task Add_Twice_Throws()
    {
        await _favorites.AddAsync(_userId, "111");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _favorites.AddAsync(_userId, "111"));
    }

    [Fact]
    public async Task Remove_NotSaved_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _favorites.RemoveAsync(_userId, "111"));
    }

    [Fact]
    public async Task GetByUser_OnlyReturnsThatUsersBooks()
    {
        var other = _db.AddUser("olena@uni.edu");
        await _favorites.AddAsync(other, "111");

        Assert.Empty(await _favorites.GetByUserAsync(_userId));
    }
}
