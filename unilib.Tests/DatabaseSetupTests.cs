using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using unilib.Data;

namespace unilib.Tests;

public class DatabaseSetupTests
{
    [Fact]
    public void Baseline_DatabaseFromEnsureCreated_RecordsOldMigrations()
    {
        using var testDb = new TestDb(); // schema made with EnsureCreated(), no history
        using var db = testDb.Create();

        Assert.True(DatabaseSetup.BaselineIfCreatedWithoutMigrations(db));
        Assert.Equal(DatabaseSetup.EnsureCreatedMigrations, db.Database.GetAppliedMigrations());

        Assert.False(DatabaseSetup.BaselineIfCreatedWithoutMigrations(db));
    }

    [Fact]
    public void Baseline_EmptyDatabase_DoesNothing()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var db = new LibraryDbContext(new DbContextOptionsBuilder<LibraryDbContext>().UseSqlite(connection).Options);

        Assert.False(DatabaseSetup.BaselineIfCreatedWithoutMigrations(db));
        Assert.Empty(db.Database.GetAppliedMigrations());
    }

    [Fact]
    public void EnsureCreatedMigrations_AreTheFirstMigrations()
    {
        using var testDb = new TestDb();
        using var db = testDb.Create();

        Assert.Equal(DatabaseSetup.EnsureCreatedMigrations,
            db.Database.GetMigrations().Take(DatabaseSetup.EnsureCreatedMigrations.Length));
    }
}
