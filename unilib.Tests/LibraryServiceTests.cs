using System.Globalization;
using Microsoft.EntityFrameworkCore;
using unilib.Models;
using unilib.Services;

namespace unilib.Tests;

public class LibraryServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly LibraryService _libraries;

    public LibraryServiceTests()
    {
        _libraries = new LibraryService(_db.Create);
    }

    public void Dispose() => _db.Dispose();

    private static decimal D(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);

    private static Library NewLibrary(decimal? latitude, decimal? longitude) =>
        new() { Name = "Central", Latitude = latitude, Longitude = longitude };

    [Fact]
    public async Task Create_WithValidCoordinates_SavesThem()
    {
        var created = await _libraries.CreateAsync(NewLibrary(49.839683m, 24.029717m));

        var stored = await _libraries.GetByIdAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Equal(49.839683m, stored.Latitude);
        Assert.Equal(24.029717m, stored.Longitude);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(-90, 180)]
    [InlineData(90, -180)]
    [InlineData(0, 0)]
    public async Task Create_WithBoundaryCoordinates_Succeeds(int latitude, int longitude)
    {
        var created = await _libraries.CreateAsync(NewLibrary(latitude, longitude));

        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Create_WithoutCoordinates_Succeeds()
    {
        var created = await _libraries.CreateAsync(NewLibrary(null, null));

        var stored = await _libraries.GetByIdAsync(created.Id);
        Assert.NotNull(stored);
        Assert.Null(stored.Latitude);
        Assert.Null(stored.Longitude);
    }

    [Theory]
    [InlineData("90.000001", "0", "Latitude")]
    [InlineData("-90.000001", "0", "Latitude")]
    [InlineData("0", "180.000001", "Longitude")]
    [InlineData("0", "-180.000001", "Longitude")]
    [InlineData("1000", "0", "Latitude")]
    public async Task Create_OutOfRange_Throws(string latitude, string longitude, string field)
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _libraries.CreateAsync(NewLibrary(D(latitude), D(longitude))));

        Assert.StartsWith($"{field} must be between", ex.Message);
        Assert.Empty(await _libraries.GetAllAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_WithOnlyOneCoordinate_Throws(bool onlyLatitude)
    {
        var library = onlyLatitude ? NewLibrary(10m, null) : NewLibrary(null, 10m);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _libraries.CreateAsync(library));

        Assert.Equal("Latitude and longitude must both be set or both be empty.", ex.Message);
    }

    [Fact]
    public async Task Update_OutOfRange_ThrowsAndKeepsStoredValues()
    {
        var created = await _libraries.CreateAsync(NewLibrary(10m, 20m));

        var invalid = NewLibrary(91m, 20m);
        invalid.Id = created.Id;
        await Assert.ThrowsAsync<InvalidOperationException>(() => _libraries.UpdateAsync(invalid));

        var stored = await _libraries.GetByIdAsync(created.Id);
        Assert.Equal(10m, stored!.Latitude);
    }

    [Fact]
    public async Task Update_WithValidCoordinates_SavesThem()
    {
        var created = await _libraries.CreateAsync(NewLibrary(10m, 20m));

        var changed = NewLibrary(-33.856784m, 151.215297m);
        changed.Id = created.Id;
        await _libraries.UpdateAsync(changed);

        var stored = await _libraries.GetByIdAsync(created.Id);
        Assert.Equal(-33.856784m, stored!.Latitude);
        Assert.Equal(151.215297m, stored.Longitude);
    }

    // Writes directly through the DbContext, skipping the service, so only the
    // database check constraints stand in the way.
    [Theory]
    [InlineData("90.5", "0")]
    [InlineData("-91", "0")]
    [InlineData("0", "180.5")]
    [InlineData("0", "-181")]
    [InlineData("10", null)]
    [InlineData(null, "10")]
    public void Database_RejectsInvalidCoordinates(string? latitude, string? longitude)
    {
        using var db = _db.Create();
        db.Libraries.Add(NewLibrary(
            latitude is null ? null : D(latitude),
            longitude is null ? null : D(longitude)));

        var ex = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        Assert.Contains("CHECK constraint failed", ex.InnerException?.Message);
    }

    [Fact]
    public void Database_AcceptsBoundaryCoordinates()
    {
        using var db = _db.Create();
        db.Libraries.Add(NewLibrary(-90m, 180m));

        db.SaveChanges();
    }
}
