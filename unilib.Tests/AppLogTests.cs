using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace unilib.Tests;

public class AppLogTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("unilib-logs-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private ILoggerFactory CreateFactory(string? minimumLevel = null) =>
        AppLog.CreateFactory(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:Directory"] = _dir,
                ["Logging:MinimumLevel"] = minimumLevel
            })
            .Build(), out _);

    private string ReadLog() => string.Concat(Directory.GetFiles(_dir, "unilib-*.log").Select(File.ReadAllText));

    [Fact]
    public void WritesEntriesToADailyFileInTheConfiguredFolder()
    {
        using (var factory = CreateFactory())
            factory.CreateLogger("unilib.Services.Test").LogInformation("Loan {LoanId} created", 42);

        var log = ReadLog();
        Assert.Contains("[INF] unilib.Services.Test: Loan 42 created", log);
        Assert.Matches(@"unilib-\d{8}\.log$", Directory.GetFiles(_dir).Single());
    }

    [Fact]
    public void WritesExceptionsWithStackTrace()
    {
        using (var factory = CreateFactory())
            factory.CreateLogger("unilib.Test").LogError(new InvalidOperationException("boom"), "Save failed");

        var log = ReadLog();
        Assert.Contains("[ERR] unilib.Test: Save failed", log);
        Assert.Contains("System.InvalidOperationException: boom", log);
    }

    [Fact]
    public void SkipsEntriesBelowTheMinimumLevel()
    {
        using (var factory = CreateFactory("Warning"))
        {
            var logger = factory.CreateLogger("unilib.Test");
            logger.LogInformation("hidden");
            logger.LogWarning("shown");
        }

        var log = ReadLog();
        Assert.DoesNotContain("hidden", log);
        Assert.Contains("shown", log);
    }

    [Fact]
    public void FrameworkLogsBelowWarningAreSkipped()
    {
        using (var factory = CreateFactory("Debug"))
        {
            factory.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command").LogInformation("Executed DbCommand");
            factory.CreateLogger("Microsoft.EntityFrameworkCore.Update").LogError("Failed to save");
        }

        var log = ReadLog();
        Assert.DoesNotContain("Executed DbCommand", log);
        Assert.Contains("Failed to save", log);
    }

    [Fact]
    public void DefaultFolderIsInLocalAppData()
    {
        using var factory = AppLog.CreateFactory(new ConfigurationBuilder().Build(), out var directory);

        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UniLib", "logs");
        Assert.Equal(expected, directory);
    }
}
