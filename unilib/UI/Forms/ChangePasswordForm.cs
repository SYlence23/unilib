using System.Drawing;
using MaterialSkin.Controls;
using unilib.Services;

namespace unilib.UI.Forms;

// Dialog for the signed-in user to change their password. DialogResult.OK when changed.
public class ChangePasswordForm : MaterialForm
{
    private const int FieldWidth = 392;

    private readonly MaterialTextBox2 _current;
    private readonly MaterialTextBox2 _new;
    private readonly MaterialTextBox2 _confirm;
    private readonly Label _error;
    private readonly MaterialButton _btnUpdate;

    public ChangePasswordForm()
    {
        Text = "Change Password";
        FormStyle = FormStyles.ActionBar_48;
        Size = new Size(440, 390);
        Sizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Theme.Apply(this);
        BackColor = Theme.Ground;

        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Theme.Ground,
            Padding = new Padding(21, 20, 21, 0)
        };
        _current = Ui.TextField("Current password", FieldWidth, password: true);
        _new = Ui.TextField("New password (at least 6 characters)", FieldWidth, password: true);
        _confirm = Ui.TextField("Confirm new password", FieldWidth, password: true);
        _error = Ui.ErrorLabel(FieldWidth);
        body.Controls.AddRange(new Control[] { _current, _new, _confirm, _error });

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Theme.Ground,
            Padding = new Padding(0, 8, 16, 0)
        };
        _btnUpdate = Ui.Button("Update password", MaterialButton.MaterialButtonType.Contained);
        _btnUpdate.Margin = new Padding(0);
        _btnUpdate.Click += BtnUpdate_Click;
        var btnCancel = Ui.Button("Cancel", MaterialButton.MaterialButtonType.Text);
        btnCancel.DialogResult = DialogResult.Cancel;
        actions.Controls.Add(_btnUpdate);
        actions.Controls.Add(btnCancel);

        Controls.Add(body);
        Controls.Add(actions);
        AcceptButton = _btnUpdate;
        CancelButton = btnCancel;
    }

    private async void BtnUpdate_Click(object? sender, EventArgs e)
    {
        _error.Visible = false;
        string? error = null;
        if (_current.Text.Length == 0)
            error = "Enter your current password.";
        else if (_new.Text.Length < 6)
            error = "New password must be at least 6 characters.";
        else if (_new.Text != _confirm.Text)
            error = "Passwords do not match.";
        if (error != null)
        {
            Ui.ShowError(_error, error);
            return;
        }

        _btnUpdate.Enabled = false;
        try
        {
            await new AuthService().ChangePasswordAsync(Session.CurrentUser!.Id, _current.Text, _new.Text);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            Ui.ShowError(_error, ex.Message);
        }
        finally
        {
            if (!IsDisposed)
                _btnUpdate.Enabled = true;
        }
    }
}
