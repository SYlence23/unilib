using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace unilib.Data;

public static class DatabaseSetup
{
    // Older builds created the schema with EnsureCreated(), which matches the model after these migrations.
    public static readonly string[] EnsureCreatedMigrations =
    {
        "20261003152044_InitialCreate",
        "20261003162727_SeedRoles"
    };

    public static void Migrate(LibraryDbContext db)
    {
        BaselineIfCreatedWithoutMigrations(db);
        db.Database.Migrate();
    }

    // A database made by EnsureCreated() has the tables but no migration history, so Migrate()
    // would try to create them again. Record the migrations it already matches.
    // Returns whether the history was written.
    public static bool BaselineIfCreatedWithoutMigrations(LibraryDbContext db)
    {
        if (db.Database.GetAppliedMigrations().Any() || !HasRoleTable(db))
            return false;

        var history = db.GetService<IHistoryRepository>();
        db.Database.ExecuteSqlRaw(history.GetCreateIfNotExistsScript());
        foreach (var id in EnsureCreatedMigrations)
            db.Database.ExecuteSqlRaw(history.GetInsertScript(new HistoryRow(id, ProductInfo.GetVersion())));
        return true;
    }

    private static bool HasRoleTable(LibraryDbContext db)
    {
        try
        {
            _ = db.Roles.AsNoTracking().Any();
            return true;
        }
        catch (DbException)
        {
            return false;
        }
    }
}
