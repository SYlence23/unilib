using System.IO;
using Microsoft.EntityFrameworkCore;
using unilib.Data;
using unilib.Models;
using unilib.Services;
using unilib.UI.Forms;

namespace unilib;

static class Program
{
    private const string AdminEmail = "admin@unilib.com";

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        System.Windows.Forms.Application.ThreadException += (s, e) => {
            System.IO.File.AppendAllText("crash_log.txt", "ThreadException: " + e.Exception.ToString() + "\n");
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
            System.IO.File.AppendAllText("crash_log.txt", "UnhandledException: " + e.ExceptionObject.ToString() + "\n");
        };

        try
        {
            using var db = new LibraryDbContext();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to migrate database: " + ex.Message);
        }

        try
        {
            new AuthService().EnsureAdminAsync(AdminEmail).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to seed admin user: " + ex.Message);
        }

        try
        {
            new LoanService().MarkOverdueAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to update overdue loans: " + ex.Message);
        }

        var context = new ApplicationContext();
        ShowLogin(context, null);
        Application.Run(context);
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
