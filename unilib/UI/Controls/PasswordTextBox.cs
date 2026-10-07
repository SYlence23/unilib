using MaterialSkin.Controls;

namespace unilib.UI.Controls;

// MaterialTextBox with Password = true. MaterialSkin.2 masks it with EM_SETPASSWORDCHAR,
// after which the rich edit control never returns from EM_STREAMOUT. RichTextBox sends
// that when its handle is destroyed (to keep the text), so closing a form with a typed
// password froze the UI thread. Clearing the text first leaves nothing to stream out.
public class PasswordTextBox : MaterialTextBox
{
    public PasswordTextBox()
    {
        Password = true;
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        Clear();
        base.OnHandleDestroyed(e);
    }
}
