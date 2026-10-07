using System.Windows.Forms;
using unilib.UI.Controls;

namespace unilib.Tests;

public class PasswordTextBoxTests
{
    // Closing the login form after typing a password used to freeze the UI thread.
    [Fact]
    public void ClosingFormWithTypedPassword_DoesNotHang()
    {
        var closed = false;
        var ui = new Thread(() =>
        {
            var form = new Form();
            var box = new PasswordTextBox();
            form.Controls.Add(box);
            form.Shown += (s, e) =>
            {
                box.Text = "admin123";
                form.Close();
            };
            Application.Run(form);
            closed = true;
        }) { IsBackground = true };
        ui.SetApartmentState(ApartmentState.STA);
        ui.Start();

        // A hang never finishes, so the generous timeout only matters on a slow machine.
        Assert.True(ui.Join(TimeSpan.FromSeconds(30)) && closed, "Closing the form froze the UI thread.");
    }
}
