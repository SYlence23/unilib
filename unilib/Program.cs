using System.IO;
using unilib.UI.Forms;
using unilib.Services;

namespace unilib;

static class Program
{
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
            using (var db = new unilib.Data.LibraryDbContext())
            {
                db.Database.EnsureCreated();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to create database: " + ex.Message);
        }

        try 
        {
            using (var db = new unilib.Data.LibraryDbContext())
            {
                if (!db.Users.Any())
                {
                    var auth = new unilib.Services.AuthService(() => db);
                    auth.RegisterAsync("Admin", "User", "admin@unilib.com", "admin123").GetAwaiter().GetResult();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to seed admin user: " + ex.Message);
        }
        
        var context = new ApplicationContext();

        var loginForm = new LoginForm();
        loginForm.FormClosed += (s, e) => {
            if (Session.CurrentUser != null)
            {
                try
                {
                    var mainForm = new MainForm();
                    mainForm.FormClosed += (s2, e2) => context.ExitThread();
                    mainForm.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error launching main window: " + ex.Message + "\n" + ex.StackTrace, "Fatal Error");
                    context.ExitThread();
                }
            }
            else
            {
                context.ExitThread();
            }
        };
        
        loginForm.Show();
        Application.Run(context);
    }    
}