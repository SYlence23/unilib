using System.Drawing;

namespace unilib.UI;

public static class Theme
{
    // Light Palette Tokens
    public static readonly Color BackgroundMain = ColorTranslator.FromHtml("#F8FAFC");
    public static readonly Color Surface = ColorTranslator.FromHtml("#FFFFFF");
    public static readonly Color SurfaceHover = ColorTranslator.FromHtml("#F1F5F9");
    public static readonly Color Primary = ColorTranslator.FromHtml("#1565C0"); // Darker Blue
    public static readonly Color PrimaryHover = ColorTranslator.FromHtml("#0D47A1");
    public static readonly Color TextPrimary = ColorTranslator.FromHtml("#202124"); // Google Dark Text
    public static readonly Color TextMuted = ColorTranslator.FromHtml("#5F6368"); // Google Gray Text
    public static readonly Color Border = ColorTranslator.FromHtml("#E2E8F0");

    // Status Colors
    public static readonly Color Success = ColorTranslator.FromHtml("#059669");
    public static readonly Color SuccessBg = ColorTranslator.FromHtml("#ECFDF5");
    public static readonly Color Warning = ColorTranslator.FromHtml("#D97706");
    public static readonly Color WarningBg = ColorTranslator.FromHtml("#FFFBEB");
    public static readonly Color Danger = ColorTranslator.FromHtml("#DC2626");
    public static readonly Color DangerBg = ColorTranslator.FromHtml("#FEF2F2");

    public static Font MainFont(float size, FontStyle style = FontStyle.Regular)
    {
        return new Font("Segoe UI", size, style);
    }
}
