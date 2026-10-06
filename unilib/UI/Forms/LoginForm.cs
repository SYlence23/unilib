using System;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using unilib.Services;

namespace unilib.UI.Forms;

public class LoginForm : MaterialForm
{
    private MaterialTextBox txtEmail;
    private MaterialTextBox txtPassword;
    private MaterialButton btnLogin;
    private MaterialButton btnRegisterLink;
    private Label lblError;

    public LoginForm()
    {
        InitializeComponent();
        var materialSkinManager = MaterialSkinManager.Instance;
        materialSkinManager.AddFormToManage(this);
        materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
        materialSkinManager.ColorScheme = new ColorScheme(
            Primary.Blue800, Primary.Blue900,
            Primary.Blue700, Accent.LightBlue200, 
            TextShade.WHITE
        );
    }

    private void InitializeComponent()
    {
        this.Text = "UniLib - Login";
        this.Size = new Size(400, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;

        var panel = new Panel
        {
            Size = new Size(320, 360),
            Location = new Point(40, 70), // Push down to clear Material ActionBar
            BackColor = Theme.Surface
        };

        var lblTitle = new Label
        {
            Text = "UniLib",
            Font = Theme.MainFont(28, FontStyle.Bold),
            ForeColor = Theme.Primary,
            AutoSize = false,
            Size = new Size(320, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 15)
        };
        panel.Controls.Add(lblTitle);

        txtEmail = new MaterialTextBox { Location = new Point(30, 80), Size = new Size(260, 50), Hint = "Email" };
        panel.Controls.Add(txtEmail);

        txtPassword = new MaterialTextBox { Location = new Point(30, 150), Size = new Size(260, 50), Hint = "Password", Password = true };
        panel.Controls.Add(txtPassword);

        lblError = new Label { ForeColor = Theme.Danger, Location = new Point(30, 210), Size = new Size(260, 20), Visible = false, Font = Theme.MainFont(9) };
        panel.Controls.Add(lblError);

        btnLogin = new MaterialButton
        {
            Text = "Login",
            Location = new Point(30, 240),
            Size = new Size(260, 40),
            Type = MaterialButton.MaterialButtonType.Contained,
            UseAccentColor = false,
            AutoSize = false
        };
        btnLogin.Click += BtnLogin_Click;
        panel.Controls.Add(btnLogin);

        var btnRegisterLink = new MaterialButton
        {
            Text = "Create an account",
            Location = new Point(30, 290),
            Size = new Size(260, 36),
            Type = MaterialButton.MaterialButtonType.Text,
            UseAccentColor = true,
            AutoSize = false
        };
        btnRegisterLink.Click += LnkRegister_Click;
        panel.Controls.Add(btnRegisterLink);

        this.Controls.Add(panel);
        this.AcceptButton = btnLogin;
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        lblError.Visible = false;
        try
        {
            System.IO.File.AppendAllText("trace.log", "   - btnLogin clicked\n");
            btnLogin.Enabled = false;
            var authService = new AuthService();
            System.IO.File.AppendAllText("trace.log", "   - Calling LoginAsync\n");
            var user = await authService.LoginAsync(txtEmail.Text, txtPassword.Text);
            System.IO.File.AppendAllText("trace.log", $"   - LoginAsync returned. User null? {user == null}\n");
            
            if (user != null)
            {
                Session.CurrentUser = user;
                this.Close();
            }
            else
            {
                lblError.Text = "Invalid email or password.";
                lblError.Visible = true;
            }
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText("trace.log", $"   - EXCEPTION in Login: {ex.Message}\n");
            lblError.Text = ex.Message;
            lblError.Visible = true;
        }
        finally
        {
            btnLogin.Enabled = true;
        }
    }

    private void LnkRegister_Click(object? sender, EventArgs e)
    {
        var registerForm = new RegisterForm();
        this.Hide();
        registerForm.ShowDialog();
        this.Show();
    }
}
