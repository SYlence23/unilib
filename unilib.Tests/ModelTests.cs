using Microsoft.EntityFrameworkCore;
using unilib.Data;

namespace unilib.Tests;

public class ModelTests
{
    // Fails when a model or DbContext change has no migration yet.
    // Uses the real Npgsql setup; no database connection is opened.
    [Fact]
    public void Migrations_MatchTheModel()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseNpgsql("Host=localhost;Database=unilib_model_check")
            .UseSnakeCaseNamingConvention()
            .Options;
        using var db = new LibraryDbContext(options);

        Assert.False(db.Database.HasPendingModelChanges(),
            "The model has changes without a migration. Run: dotnet ef migrations add <Name>");
    }
}
