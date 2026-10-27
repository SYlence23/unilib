using System.Drawing;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using MaterialSkin.Controls;
using unilib.Services;

namespace unilib.UI.Forms;

// Sign-in window: the red UniLib poster on the left, and the login or register form on the right.
// Closes with Session.CurrentUser set after a successful login.
public class LoginForm : MaterialForm
{
    private const int FieldWidth = 380;

    private readonly Panel _host;
    private readonly FlowLayoutPanel _loginView;
    private readonly FlowLayoutPanel _registerView;

    private MaterialTextBox2 _loginEmail = null!;
    private MaterialTextBox2 _loginPassword = null!;
    private Label _loginNotice = null!;
    private Label _loginError = null!;
    private MaterialButton _btnLogin = null!;

    private MaterialTextBox2 _firstName = null!;
    private MaterialTextBox2 _lastName = null!;
    private MaterialTextBox2 _studentId = null!;
    private MaterialTextBox2 _registerEmail = null!;
    private MaterialTextBox2 _registerPassword = null!;
    private Label _registerError = null!;
    private MaterialButton _btnRegister = null!;

    public LoginForm(string? email = null)
    {
        Text = "UniLib - Login";
        FormStyle = FormStyles.ActionBar_None;
        Size = new Size(1000, 680);
        Sizable = false;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Theme.Apply(this);
        BackColor = Theme.Ground;

        _host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Ground };
        _loginView = BuildLoginView();
        _registerView = BuildRegisterView();
        _host.Controls.Add(_loginView);
        _host.Controls.Add(_registerView);
        _host.Resize += (s, e) => CenterViews();

        var poster = new PosterPanel { Dock = DockStyle.Left, Width = 440 };
        Ui.DragsWindow(poster);

        // The last control added is docked first: poster on the left, the form fills the rest.
        Controls.Add(_host);
        Controls.Add(poster);

        _loginEmail.Text = email ?? "";
        ShowLogin();
    }

    private FlowLayoutPanel BuildLoginView()
    {
        var view = NewView();
        view.Controls.Add(Ui.Overline("UniLib - Login"));
        view.Controls.Add(Title("Log in"));

        _loginNotice = new Label
        {
            AutoSize = false,
            Size = new Size(FieldWidth, 40),
            Padding = new Padding(12, 0, 12, 0),
            Margin = new Padding(0, 0, 0, 16),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Theme.Accent100,
            ForeColor = Theme.Accent800,
            Font = Theme.Font(13),
            Visible = false
        };
        view.Controls.Add(_loginNotice);

        _loginEmail = Ui.TextField("Email", FieldWidth);
        _loginPassword = PasswordField("Password");
        _loginError = Ui.ErrorLabel(FieldWidth);
        view.Controls.Add(_loginEmail);
        view.Controls.Add(_loginPassword);
        view.Controls.Add(_loginError);

        _btnLogin = WideButton("Login", MaterialButton.MaterialButtonType.Contained);
        _btnLogin.Click += BtnLogin_Click;
        view.Controls.Add(_btnLogin);

        var rule = Ui.Rule();
        rule.Dock = DockStyle.None;
        rule.Width = FieldWidth;
        rule.Margin = new Padding(0, 16, 0, 16);
        view.Controls.Add(rule);

        var btnCreate = WideButton("Create an account", MaterialButton.MaterialButtonType.Outlined);
        btnCreate.Click += (s, e) => ShowRegister();
        view.Controls.Add(btnCreate);

        var demo = Ui.Text("Demo: admin@unilib.com / admin123", 12, FontStyle.Regular, Theme.TextMuted);
        demo.Margin = new Padding(0, 16, 0, 0);
        view.Controls.Add(demo);
        return view;
    }

    private FlowLayoutPanel BuildRegisterView()
    {
        var view = NewView();
        view.Controls.Add(Ui.Overline("UniLib - Register"));
        view.Controls.Add(Title("Register"));

        var names = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        var half = (FieldWidth - 12) / 2;
        _firstName = Ui.TextField("First Name", half);
        _firstName.Margin = new Padding(0, 0, 12, 12);
        _lastName = Ui.TextField("Last Name", half);
        names.Controls.Add(_firstName);
        names.Controls.Add(_lastName);
        view.Controls.Add(names);

        _studentId = Ui.TextField("Student ID (Optional)", FieldWidth);
        _registerEmail = Ui.TextField("Email", FieldWidth);
        _registerPassword = PasswordField("Password (at least 6 characters)");
        _registerError = Ui.ErrorLabel(FieldWidth);
        view.Controls.Add(_studentId);
        view.Controls.Add(_registerEmail);
        view.Controls.Add(_registerPassword);
        view.Controls.Add(_registerError);

        _btnRegister = WideButton("Create Account", MaterialButton.MaterialButtonType.Contained);
        _btnRegister.Click += BtnRegister_Click;
        view.Controls.Add(_btnRegister);

        var btnBack = Ui.Button("Already have an account? Log in", MaterialButton.MaterialButtonType.Text);
        btnBack.Margin = new Padding(0, 12, 0, 0);
        btnBack.Click += (s, e) => ShowLogin();
        view.Controls.Add(btnBack);
        return view;
    }

    private static FlowLayoutPanel NewView() => new()
    {
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = Color.Transparent
    };

    private static Label Title(string text)
    {
        var title = Ui.Text(text, 32, FontStyle.Bold);
        title.Margin = new Padding(0, 6, 0, 20);
        return title;
    }

    private static MaterialButton WideButton(string text, MaterialButton.MaterialButtonType type)
    {
        var button = Ui.Button(text, type);
        button.AutoSize = false;
        button.Size = new Size(FieldWidth, 42);
        button.Margin = new Padding(0);
        return button;
    }

    // Password field with an eye icon that shows or hides the text.
    private static MaterialTextBox2 PasswordField(string hint)
    {
        var field = Ui.TextField(hint, FieldWidth, password: true);
        field.TrailingIcon = Icons.Bitmap(Glyph.Eye, Color.Black);
        field.TrailingIconClick += (s, e) =>
        {
            field.UseSystemPasswordChar = !field.UseSystemPasswordChar;
            field.TrailingIcon = Icons.Bitmap(field.UseSystemPasswordChar ? Glyph.Eye : Glyph.EyeOff, Color.Black);
        };
        return field;
    }

    private void ShowLogin(string? notice = null)
    {
        _registerView.Visible = false;
        _loginView.Visible = true;
        _loginError.Visible = false;
        _loginNotice.Text = notice ?? "";
        _loginNotice.Visible = notice != null;
        AcceptButton = _btnLogin;
        CenterViews();
        (_loginEmail.Text.Length == 0 ? _loginEmail : _loginPassword).Focus();
    }

    private void ShowRegister()
    {
        _loginView.Visible = false;
        _registerView.Visible = true;
        _registerError.Visible = false;
        AcceptButton = _btnRegister;
        CenterViews();
        _firstName.Focus();
    }

    private void CenterViews()
    {
        foreach (var view in new[] { _loginView, _registerView })
        {
            view.PerformLayout();
            view.Location = new Point(
                Math.Max(24, (_host.ClientSize.Width - view.Width) / 2),
                Math.Max(24, (_host.ClientSize.Height - view.Height) / 2));
        }
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        _loginError.Visible = false;
        if (string.IsNullOrWhiteSpace(_loginEmail.Text) || string.IsNullOrEmpty(_loginPassword.Text))
        {
            Ui.ShowError(_loginError, "Enter your email and password.");
            return;
        }

        _btnLogin.Enabled = false;
        try
        {
            var user = await new AuthService().LoginAsync(_loginEmail.Text, _loginPassword.Text);
            if (user == null)
            {
                Ui.ShowError(_loginError, "Invalid email or password.");
                return;
            }
            Session.CurrentUser = user;
            Close();
        }
        catch (Exception ex)
        {
            Ui.ShowError(_loginError, ex.Message);
        }
        finally
        {
            if (!IsDisposed)
                _btnLogin.Enabled = true;
        }
    }

    private async void BtnRegister_Click(object? sender, EventArgs e)
    {
        _registerError.Visible = false;
        var email = _registerEmail.Text.Trim().ToLowerInvariant();

        string? error = null;
        if (string.IsNullOrWhiteSpace(_firstName.Text) || string.IsNullOrWhiteSpace(_lastName.Text))
            error = "First and last name are required.";
        else if (!Regex.IsMatch(email, @"^\S+@\S+\.\S+$"))
            error = "Enter a valid email address.";
        else if (_registerPassword.Text.Length < 6)
            error = "Password must be at least 6 characters.";
        if (error != null)
        {
            Ui.ShowError(_registerError, error);
            return;
        }

        _btnRegister.Enabled = false;
        try
        {
            await new AuthService().RegisterAsync(
                _firstName.Text, _lastName.Text, email, _registerPassword.Text, _studentId.Text);

            foreach (var field in new[] { _firstName, _lastName, _studentId, _registerEmail, _registerPassword })
                field.Text = "";
            _loginEmail.Text = email;
            _loginPassword.Text = "";
            ShowLogin("Registration successful! You can now log in.");
        }
        catch (Exception ex)
        {
            Ui.ShowError(_registerError, ex.Message);
        }
        finally
        {
            _btnRegister.Enabled = true;
        }
    }

    // The accent poster: "University Library", a large "Uni Lib" and the three sections.
    private class PosterPanel : Panel
    {
        private static readonly string[] Sections = { "Catalog", "Reservations", "Campus Map" };

        public PosterPanel()
        {
            BackColor = Theme.Accent;
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using var ink = new SolidBrush(Theme.Ground);
            using var rule = new Pen(Theme.Ground, 2);
            const int pad = 48;

            g.DrawString("UNIVERSITY LIBRARY", Theme.Font(13, FontStyle.Bold), ink, pad - 2, 40);

            var big = Theme.Font(150, FontStyle.Bold);
            var top = Height / 2 - 170;
            g.DrawString("Uni", big, ink, pad - 14, top);
            g.DrawString("Lib", big, ink, pad - 14, top + 130);

            var lineY = Height - 96;
            g.DrawLine(rule, pad, lineY, Width - pad, lineY);
            var column = (Width - 2 * pad) / Sections.Length;
            for (var i = 0; i < Sections.Length; i++)
            {
                var x = pad + i * column;
                if (i > 0)
                    g.DrawLine(rule, x, lineY, x, Height - 40);
                var textX = i == 0 ? x : x + 12;
                g.DrawString($"0{i + 1}", Theme.Font(11, FontStyle.Bold), ink, textX, lineY + 12);
                g.DrawString(Sections[i], Theme.Font(15, FontStyle.Bold), ink, textX, lineY + 30);
            }
        }
    }
}
