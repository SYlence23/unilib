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
        
        File.AppendAllText("trace.log", "1. Setup done\n");
        var loginForm = new LoginForm();
        File.AppendAllText("trace.log", "2. Showing LoginForm\n");
        var result = loginForm.ShowDialog();
        File.AppendAllText("trace.log", $"3. LoginForm closed with result: {result}\n");
        
        if (result == DialogResult.OK || Session.CurrentUser != null)
        {
            File.AppendAllText("trace.log", $"4. Condition met. Session is null? {Session.CurrentUser == null}\n");
            try
            {
                File.AppendAllText("trace.log", "5. Instantiating MainForm\n");
                var mainForm = new MainForm();
                File.AppendAllText("trace.log", "6. Running Application with MainForm\n");
                Application.Run(mainForm);
                File.AppendAllText("trace.log", "7. Application Run finished normally\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("trace.log", $"EXCEPTION: {ex}\n");
                MessageBox.Show("Error launching main window: " + ex.Message + "\n" + ex.StackTrace, "Fatal Error");
            }
        }
        else
        {
            File.AppendAllText("trace.log", "4. Condition NOT met, exiting app\n");
            Application.Exit();
        }
    }    
}