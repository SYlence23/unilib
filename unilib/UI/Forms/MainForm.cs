using System.Drawing;
using MaterialSkin.Controls;
using unilib.Models;
using unilib.Services;
using unilib.UI.Controls;

namespace unilib.UI.Forms;

// The app shell: header, numbered side navigation, the current screen and a status footer.
public class MainForm : MaterialForm
{
    private enum Screen { Catalog, Profile, Map, Admin }

    private readonly Panel _content;
    private readonly Panel _sidebar;
    private readonly Dictionary<Screen, NavButton> _nav = new();
    private Control? _current;

    public MainForm()
    {
        var user = Session.CurrentUser ?? throw new InvalidOperationException("No user is logged in.");

        Text = "UniLib - Desktop";
        FormStyle = FormStyles.ActionBar_None;
        Size = new Size(1440, 900);
        MinimumSize = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterScreen;
        Theme.Apply(this);
        BackColor = Theme.Ground;

        _content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Ground, Padding = new Padding(40, 32, 40, 24) };
        _sidebar = BuildSidebar(user);

        // The last control added is docked first: header and footer span the full width,
        // the sidebar takes the left of what remains, the screen fills the rest.
        Controls.Add(_content);
        Controls.Add(_sidebar);
        Controls.Add(BuildFooter(user));
        Controls.Add(BuildHeader(user));

        ShowScreen(Screen.Catalog);
    }

    // True when the window closed because the user signed out (rather than quitting).
    public bool SignedOut { get; private set; }

    private Control BuildHeader(User user)
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Theme.Ground };
        header.Controls.Add(Ui.Rule(DockStyle.Bottom));

        var btnMenu = new IconButton(Glyph.Menu, "Toggle menu", 58) { Location = new Point(0, 0) };
        btnMenu.Click += (s, e) => _sidebar.Visible = !_sidebar.Visible;
        header.Controls.Add(btnMenu);
        header.Controls.Add(new Panel { BackColor = Theme.Divider, Bounds = new Rectangle(58, 0, 2, 58) });

        var brand = Ui.Text("UniLib", 22, FontStyle.Bold);
        brand.Location = new Point(80, 15);
        header.Controls.Add(brand);

        var account = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = Color.Transparent,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        var name = Ui.Text($"{user.FirstName} {user.LastName}", 14, FontStyle.Bold);
        name.Margin = new Padding(0, 10, 12, 0);
        var role = new Chip(Ui.RoleLabel(user), ChipKind.Outline) { Margin = new Padding(0, 8, 12, 0) };
        var btnSignOut = Ui.Button("Sign out", MaterialButton.MaterialButtonType.Text, Glyph.LogOut);
        btnSignOut.Margin = new Padding(0);
        btnSignOut.Click += (s, e) => SignOut();
        account.Controls.Add(name);
        account.Controls.Add(role);
        account.Controls.Add(btnSignOut);
        header.Controls.Add(account);

        void PlaceAccount() => account.Location =
            new Point(header.ClientSize.Width - account.Width - 16, (header.Height - 2 - account.Height) / 2);
        header.Resize += (s, e) => PlaceAccount();
        account.SizeChanged += (s, e) => PlaceAccount();

        Ui.DragsWindow(header);
        Ui.DragsWindow(brand);
        return header;
    }

    private Panel BuildSidebar(User user)
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = Theme.Ground };

        var items = new List<(Screen Screen, string Number, string Label)>
        {
            (Screen.Catalog, "01", "Catalog"),
            (Screen.Profile, "02", "Student Profile"),
            (Screen.Map, "03", "Campus Map")
        };
        if (Ui.IsStaff(user))
            items.Add((Screen.Admin, "04", "Admin Control"));

        // Top-docked controls stack in reverse order of adding, so add the last item first.
        for (var i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            var button = new NavButton(item.Label, item.Number) { Dock = DockStyle.Top, AccessibleName = item.Label };
            button.Click += (s, e) => ShowScreen(item.Screen);
            _nav[item.Screen] = button;
            sidebar.Controls.Add(button);
        }
        sidebar.Controls.Add(Ui.Rule(DockStyle.Right));
        return sidebar;
    }

    private static Control BuildFooter(User user)
    {
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = Theme.Ground, Padding = new Padding(16, 2, 16, 0) };
        var left = Ui.Text($"Logged in as: {user.FirstName} {user.LastName}", 12, FontStyle.Regular, Theme.TextMuted);
        var right = Ui.Text("UniLib - Desktop", 12, FontStyle.Regular, Theme.TextMuted);
        left.AutoSize = right.AutoSize = false;
        left.Width = right.Width = 300;
        left.TextAlign = ContentAlignment.MiddleLeft;
        right.TextAlign = ContentAlignment.MiddleRight;
        left.Dock = DockStyle.Left;
        right.Dock = DockStyle.Right;
        footer.Controls.Add(left);
        footer.Controls.Add(right);
        footer.Controls.Add(Ui.Rule());
        return footer;
    }

    private void ShowScreen(Screen screen, Book? locate = null)
    {
        foreach (var (key, button) in _nav)
            button.Active = key == screen;

        // Requests from inside a screen are deferred: switching disposes that screen,
        // including the button whose Click is still running.
        Control view;
        switch (screen)
        {
            case Screen.Profile:
                var profile = new ProfileControl();
                profile.BrowseCatalogRequested += (s, e) => BeginInvoke(() => ShowScreen(Screen.Catalog));
                profile.LocateRequested += (s, book) => BeginInvoke(() => ShowScreen(Screen.Map, book));
                view = profile;
                break;
            case Screen.Map:
                view = new MapControl(locate);
                break;
            case Screen.Admin:
                view = new AdminControl();
                break;
            default:
                var catalog = new CatalogControl();
                catalog.LocateRequested += (s, book) => BeginInvoke(() => ShowScreen(Screen.Map, book));
                view = catalog;
                break;
        }

        // Each visit builds the screen afresh, so it always shows current data.
        if (_current != null)
        {
            _content.Controls.Remove(_current);
            _current.Dispose();
        }
        view.Dock = DockStyle.Fill;
        _content.Controls.Add(view);
        _current = view;
    }

    private void SignOut()
    {
        Session.Logout();
        SignedOut = true;
        Close();
    }
}
