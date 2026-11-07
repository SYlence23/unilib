using System.IO;
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
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to create database: " + ex.Message);
        }

        try
        {
            EnsureAdminAccount();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to seed admin user: " + ex.Message);
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

    // Makes sure the database has an admin. Once any admin exists this does nothing.
    // Otherwise it creates admin@unilib.com with the default password (which must be changed
    // at first login) and gives it the admin role (RegisterAsync always creates students).
    // An existing admin@unilib.com is promoted only while it still has the default password:
    // that is the seeded account (older versions created it as a student), not someone who
    // registered the address with their own password.
    private static void EnsureAdminAccount()
    {
        using var db = new LibraryDbContext();
        var adminRole = db.Roles.SingleOrDefault(r => r.Name == RoleNames.Admin);
        if (adminRole == null || db.Users.Any(u => u.RoleId == adminRole.Id))
            return;

        var auth = new AuthService();
        if (!db.Users.Any(u => u.Email == AdminEmail))
            auth.RegisterAsync("Admin", "User", AdminEmail, AuthService.DefaultAdminPassword).GetAwaiter().GetResult();
        else if (auth.LoginAsync(AdminEmail, AuthService.DefaultAdminPassword).GetAwaiter().GetResult() == null)
            return;

        var admin = db.Users.Single(u => u.Email == AdminEmail);
        admin.RoleId = adminRole.Id;
        db.SaveChanges();
    }
}
