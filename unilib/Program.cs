using Microsoft.Extensions.Logging;
using unilib.Data;
using unilib.Services;
using unilib.UI.Forms;

namespace unilib;

static class Program
{
    private const string AdminEmail = "admin@unilib.com";

    private static ILogger _log = AppLog.CreateLogger(typeof(Program));

    [STAThread]
    static void Main()
    {
        AppLog.Configure(AppConfig.Configuration);
        _log = AppLog.CreateLogger(typeof(Program));
        _log.LogInformation("UniLib {Version} starting on {OS}", Application.ProductVersion, Environment.OSVersion);

        // Errors no handler caught: log them instead of losing them.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) => ReportUnexpectedError(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            _log.LogCritical(e.ExceptionObject as Exception, "Unhandled exception, UniLib is terminating");
            AppLog.Shutdown();
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            _log.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        try
        {
            Run();
        }
        finally
        {
            _log.LogInformation("UniLib stopped");
            AppLog.Shutdown();
        }
    }

    private static void Run()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            using var db = new LibraryDbContext();
            DatabaseSetup.Migrate(db);
            _log.LogInformation("Database schema is up to date");
        }
        catch (Exception ex)
        {
            // Running on an old schema would crash later, so stop here.
            _log.LogCritical(ex, "Database migration failed, UniLib cannot start");
            MessageBox.Show("Failed to update the database, so UniLib cannot start.\n\n" + ex.Message,
                "UniLib", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            new AuthService().EnsureAdminAsync(AdminEmail).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Could not make sure an admin account exists");
            MessageBox.Show("Failed to seed admin user: " + ex.Message);
        }

        try
        {
            new LoanService().MarkOverdueAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Could not mark overdue loans");
            MessageBox.Show("Failed to update overdue loans: " + ex.Message);
        }

        var context = new ApplicationContext();
        ShowLogin(context, null);
        Application.Run(context);
    }

    // An exception from an event handler that nothing caught. The app keeps running.
    private static void ReportUnexpectedError(Exception ex)
    {
        _log.LogError(ex, "Unhandled exception on the UI thread");
        MessageBox.Show(
            "Something went wrong. UniLib can keep running, but the last action may not have finished.\n\n" +
            $"Details were written to the log in {AppLog.Directory}.",
            "UniLib", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // The login and main windows take turns: signing out goes back to the login window,
    // closing either window quits the app.
    private static void ShowLogin(ApplicationContext context, string? email)
    {
        var loginForm = new LoginForm(email);
        loginForm.FormClosed += (s, e) =>
        {
            if (Session.IsLoggedIn)
                ShowMain(context);
            else
                context.ExitThread();
        };
        loginForm.Show();
    }

    private static void ShowMain(ApplicationContext context)
    {
        var email = Session.CurrentUser!.Email;
        MainForm mainForm;
        try
        {
            mainForm = new MainForm();
        }
        catch (Exception ex)
        {
            _log.LogCritical(ex, "Could not open the main window");
            MessageBox.Show("Error launching main window: " + ex.Message + "\n" + ex.StackTrace, "Fatal Error");
            context.ExitThread();
            return;
        }

        mainForm.FormClosed += (s, e) =>
        {
            if (mainForm.SignedOut)
                ShowLogin(context, email);
            else
                context.ExitThread();
        };
        mainForm.Show();
    }
}
