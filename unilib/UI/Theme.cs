using System.Drawing;
using MaterialSkin;
using MaterialSkin.Controls;

namespace unilib.UI;

// Colors and type from the UniLib design ("Modernist": ink on a light ground,
// a single red accent, square corners, strong 2px rules), mapped onto MaterialSkin.
public static class Theme
{
    public static readonly Color Ground = ColorTranslator.FromHtml("#F3F2F2");
    public static readonly Color Surface = ColorTranslator.FromHtml("#EAE9E9");
    public static readonly Color Card = Color.White;
    public static readonly Color Ink = ColorTranslator.FromHtml("#201E1D");
    // --color-divider is ink at 40% over the ground.
    public static readonly Color Divider = ColorTranslator.FromHtml("#9F9D9D");

    public static readonly Color Accent = ColorTranslator.FromHtml("#EC3013");
    public static readonly Color Accent100 = ColorTranslator.FromHtml("#FFF2EF");
    public static readonly Color Accent300 = ColorTranslator.FromHtml("#FFC4B8");
    public static readonly Color Accent700 = ColorTranslator.FromHtml("#AE1800");
    public static readonly Color Accent800 = ColorTranslator.FromHtml("#7C1405");

    public static readonly Color Neutral100 = ColorTranslator.FromHtml("#F8F4F4");
    public static readonly Color Neutral300 = ColorTranslator.FromHtml("#D7D3D3");
    public static readonly Color Neutral400 = ColorTranslator.FromHtml("#BAB6B6");
    public static readonly Color Neutral700 = ColorTranslator.FromHtml("#605D5D");
    public static readonly Color Neutral800 = ColorTranslator.FromHtml("#444141");

    // Hover tint for flat controls: ink at 7% over the ground.
    public static readonly Color Hover = ColorTranslator.FromHtml("#E3E2E2");

    public static readonly Color TextPrimary = Ink;
    public static readonly Color TextMuted = Neutral700;

    private static FontFamily? _family;
    private static readonly Dictionary<(float, FontStyle), Font> Fonts = new();

    // Roboto from MaterialSkin's embedded fonts, sized in pixels like the design.
    // Fonts are cached: controls share them and they live as long as the app.
    public static Font Font(float px, FontStyle style = FontStyle.Regular)
    {
        if (Fonts.TryGetValue((px, style), out var cached))
            return cached;

        _family ??= MaterialSkinManager.Instance.getFontByType(MaterialSkinManager.fontType.Body1).FontFamily;
        var font = new Font(_family, px, style, GraphicsUnit.Pixel);
        Fonts[(px, style)] = font;
        return font;
    }

    // Registers the form with MaterialSkin and applies the UniLib color scheme.
    public static void Apply(MaterialForm form)
    {
        var skin = MaterialSkinManager.Instance;
        // Otherwise MaterialSkin repaints every plain control (panels, labels) with its
        // own background, foreground and font whenever it is added to the form.
        skin.EnforceBackcolorOnAllComponents = false;
        skin.Theme = MaterialSkinManager.Themes.LIGHT;
        skin.ColorScheme = new ColorScheme(Accent, Accent700, Accent300, Ink, TextShade.WHITE);
        skin.AddFormToManage(form);
        form.FormClosed += (s, e) => skin.RemoveFormToManage(form);
    }
}
