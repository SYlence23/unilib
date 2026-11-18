using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using unilib.Models;
using unilib.Services;
using unilib.UI;

namespace unilib.Tests;

public class ErrorMessagesTests
{
    [Fact]
    public void ServiceException_ShowsItsMessage() =>
        Assert.Equal("Loan limit reached.", ErrorMessages.ForUser(new ServiceException("Loan limit reached.")));

    [Theory]
    [MemberData(nameof(ConnectionFailures))]
    public void ConnectionFailure_ShowsDatabaseUnavailable(Exception ex) =>
        Assert.Equal(ErrorMessages.DatabaseUnavailable, ErrorMessages.ForUser(ex));

    public static TheoryData<Exception> ConnectionFailures() => new()
    {
        new NpgsqlException("Failed to connect to 127.0.0.1:5432"),
        new PostgresException("password authentication failed for user \"postgres\"", "FATAL", "FATAL", "28P01"),
        new PostgresException("database \"unilib\" does not exist", "FATAL", "FATAL", "3D000"),
        // EF Core wraps connection failures of a query.
        new InvalidOperationException("An exception has been raised that is likely due to a transient failure.",
            new NpgsqlException("Exception while reading from stream", new SocketException())),
        new TimeoutException("The operation has timed out.")
    };

    [Theory]
    [MemberData(nameof(OtherFailures))]
    public void OtherFailure_ShowsGenericMessageWithoutDetails(Exception ex)
    {
        var text = ErrorMessages.ForUser(ex);

        Assert.Equal(ErrorMessages.Unexpected, text);
        Assert.DoesNotContain(ex.Message, text);
    }

    public static TheoryData<Exception> OtherFailures() => new()
    {
        new PostgresException("relation \"user\" does not exist", "ERROR", "ERROR", "42P01"),
        new InvalidOperationException("Sequence contains more than one element"),
        new NullReferenceException(),
        new DbUpdateException("An error occurred while saving the entity changes.")
    };

    [Fact]
    public void Report_LogsUnexpectedErrorsWithTheException()
    {
        var log = new ListLogger<ErrorMessagesTests>();
        var ex = new NullReferenceException();

        ErrorMessages.Report(ex, "Loading the catalog", log);

        var entry = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("Loading the catalog failed", entry.Message);
        Assert.Same(ex, entry.Exception);
    }

    [Fact]
    public void Report_LogsRefusalsAsInformation()
    {
        var log = new ListLogger<ErrorMessagesTests>();

        var text = ErrorMessages.Report(new ServiceException("Loan limit reached."), "Borrowing a book", log);

        Assert.Equal("Loan limit reached.", text);
        var entry = Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("Borrowing a book refused: Loan limit reached.", entry.Message);
    }

    [Fact]
    public async Task FailedSave_HidesDatabaseDetailsButKeepsThemForTheLog()
    {
        using var testDb = new TestDb();
        var userId = testDb.AddUser();

        // The book does not exist, so the foreign key rejects the favorite.
        var ex = await Assert.ThrowsAsync<ServiceException>(
            () => new FavoriteService(testDb.Create).AddAsync(userId, "no-such-book"));

        Assert.Equal("Could not save the favorite. Check the entered data and try again.", ex.Message);
        Assert.IsType<DbUpdateException>(ex.InnerException);
        Assert.Contains("FOREIGN KEY", ex.InnerException!.InnerException!.Message);
        Assert.Equal(ex.Message, ErrorMessages.ForUser(ex));
    }

    [Fact]
    public async Task NotFound_IsAServiceException()
    {
        using var testDb = new TestDb();

        var ex = await Assert.ThrowsAsync<ServiceException>(() => new LoanService(testDb.Create).ReturnAsync(404));

        Assert.Equal($"{nameof(UserBook)} '404' was not found.", ex.Message);
    }
}
