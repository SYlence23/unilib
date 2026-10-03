using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using unilib.Data;

namespace unilib.Tests;

// In-memory SQLite database that lives as long as this object.
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<LibraryDbContext> _options;

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = Create();
        db.Database.EnsureCreated();
    }

    public LibraryDbContext Create() => new(_options);

    public void Dispose() => _connection.Dispose();
}
