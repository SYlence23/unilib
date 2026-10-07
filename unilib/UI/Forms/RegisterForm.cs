using System;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using unilib.Services;
using unilib.UI.Controls;

namespace unilib.UI.Forms;

public class RegisterForm : MaterialForm
{
    private MaterialTextBox txtFirstName;
    private MaterialTextBox txtLastName;
    private MaterialTextBox txtEmail;
    private PasswordTextBox txtPassword;
    private MaterialTextBox txtStudentId;
    private MaterialButton btnRegister;
    private Label lblError;

    public RegisterForm()
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
        this.Text = "UniLib - Register";
        this.Size = new Size(400, 680);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;

        var panel = new Panel
        {
            Size = new Size(320, 580),
            Location = new Point(40, 70), // Push down for Material App Bar
            BackColor = Theme.Surface
        };

        var lblTitle = new Label 
        { 
            Text = "Register", 
            Font = Theme.MainFont(28, FontStyle.Bold), 
            ForeColor = Theme.Primary, 
            AutoSize = false,
            Size = new Size(320, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 10) 
        };
        panel.Controls.Add(lblTitle);

        int y = 70; // Push fields down slightly to clear the bigger title
        int spacing = 65;

        txtFirstName = AddField(panel, "First Name", ref y, spacing);
        txtLastName = AddField(panel, "Last Name", ref y, spacing);
        txtStudentId = AddField(panel, "Student ID (Optional)", ref y, spacing);
        txtEmail = AddField(panel, "Email", ref y, spacing);
        
        txtPassword = new PasswordTextBox { Location = new Point(30, y), Size = new Size(260, 50), Hint = "Password" };
        panel.Controls.Add(txtPassword);
        y += spacing;

        lblError = new Label { ForeColor = Theme.Danger, Location = new Point(30, y), Size = new Size(260, 20), Visible = false, Font = Theme.MainFont(9) };
        panel.Controls.Add(lblError);
        y += 25;

        btnRegister = new MaterialButton
        {
            Text = "Create Account",
            Location = new Point(30, y),
            Size = new Size(260, 40),
            Type = MaterialButton.MaterialButtonType.Contained,
            UseAccentColor = false,
            AutoSize = false
        };
        btnRegister.Click += BtnRegister_Click;
        panel.Controls.Add(btnRegister);
        y += 50;

        var btnLoginLink = new MaterialButton
        {
            Text = "ALREADY HAVE AN ACCOUNT? LOGIN",
            Location = new Point(0, y),
            Size = new Size(320, 36),
            Type = MaterialButton.MaterialButtonType.Text,
            UseAccentColor = true,
            AutoSize = false
        };
        btnLoginLink.Click += (s, e) => this.Close(); // Return to login
        panel.Controls.Add(btnLoginLink);

        this.Controls.Add(panel);
    }

    private MaterialTextBox AddField(Panel parent, string hint, ref int y, int spacing)
    {
        var txt = new MaterialTextBox { Location = new Point(30, y), Size = new Size(260, 50), Hint = hint };
        parent.Controls.Add(txt);
        y += spacing;
        return txt;
    }

    private async void BtnRegister_Click(object? sender, EventArgs e)
    {
        lblError.Visible = false;
        try
        {
            btnRegister.Enabled = false;
            var authService = new AuthService();
            await authService.RegisterAsync(txtFirstName.Text, txtLastName.Text, txtEmail.Text, txtPassword.Text, txtStudentId.Text);
            
            MessageBox.Show("Registration successful! You can now log in.", "Success");
            this.Close(); // Close registration, return to login form
        }
        catch (Exception ex)
        {
            lblError.Text = ex.Message;
            lblError.Visible = true;
        }
        finally
        {
            btnRegister.Enabled = true;
        }
    }
}
