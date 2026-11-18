using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Npgsql;
using unilib.Services;

namespace unilib.UI;

// Turns a failed action into the text the user sees, and logs it.
// A ServiceException message is written for users, so it is shown as is. Anything else
// is a bug or an infrastructure problem: its text can name tables, hosts or code, so the
// user gets a short generic message and the details go to the log.
public static class ErrorMessages
{
    public const string DatabaseUnavailable =
        "Cannot connect to the database. Check the connection and try again.";

    public const string Unexpected =
        "Something went wrong. The details were written to the log.";

    // PostgreSQL errors that mean the connection settings are wrong: bad password,
    // unknown database, server starting up or shutting down.
    private static readonly HashSet<string> ConnectionSqlStates = new() { "28000", "28P01", "3D000", "57P03" };

    public static string ForUser(Exception ex) => ex switch
    {
        ServiceException => ex.Message,
        _ when IsConnectionProblem(ex) => DatabaseUnavailable,
        _ => Unexpected
    };

    // Logs the failure of `action` ("Loading the catalog") and returns the text to show.
    public static string Report(Exception ex, string action, ILogger? logger = null)
    {
        logger ??= AppLog.CreateLogger(typeof(ErrorMessages));
        if (ex is ServiceException)
            logger.LogInformation("{Action} refused: {Reason}", action, ex.Message);
        else if (IsConnectionProblem(ex))
            logger.LogError(ex, "{Action} failed: the database is unavailable", action);
        else
            logger.LogError(ex, "{Action} failed", action);
        return ForUser(ex);
    }

    public static bool IsConnectionProblem(Exception ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
        {
            switch (e)
            {
                case PostgresException pg:
                    if (ConnectionSqlStates.Contains(pg.SqlState))
                        return true;
                    break;
                // Npgsql reports network and protocol failures as NpgsqlException;
                // errors returned by the server are PostgresException.
                case NpgsqlException:
                case SocketException:
                case TimeoutException:
                    return true;
            }
        }
        return false;
    }
}
