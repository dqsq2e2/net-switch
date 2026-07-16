using Microsoft.Win32;

namespace NetAdapterSwitcher;

internal sealed record AppTheme(
    bool IsDark,
    Color Window,
    Color Panel,
    Color PanelAlt,
    Color Border,
    Color Text,
    Color SubText,
    Color Button,
    Color ButtonText,
    Color Primary,
    Color PrimaryText,
    Color Card,
    Color CardText,
    Color CardSubText,
    Color InactiveText,
    Color Grid,
    Color GridHeader,
    Color Selection);

internal static class ThemeService
{
    private const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool IsDarkMode()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        object? value = key?.GetValue("AppsUseLightTheme");
        return value is int intValue && intValue == 0;
    }

    public static AppTheme Current => IsDarkMode() ? Dark : Light;

    public static readonly AppTheme Light = new(
        IsDark: false,
        Window: Color.FromArgb(245, 247, 250),
        Panel: Color.White,
        PanelAlt: Color.FromArgb(247, 248, 250),
        Border: Color.FromArgb(210, 214, 220),
        Text: Color.FromArgb(30, 35, 43),
        SubText: Color.FromArgb(100, 108, 120),
        Button: Color.FromArgb(237, 240, 244),
        ButtonText: Color.FromArgb(42, 51, 64),
        Primary: Color.FromArgb(226, 237, 255),
        PrimaryText: Color.FromArgb(35, 92, 205),
        Card: Color.White,
        CardText: Color.FromArgb(31, 37, 46),
        CardSubText: Color.FromArgb(87, 96, 109),
        InactiveText: Color.FromArgb(145, 151, 162),
        Grid: Color.White,
        GridHeader: Color.FromArgb(247, 249, 251),
        Selection: Color.FromArgb(226, 237, 255));

    public static readonly AppTheme Dark = new(
        IsDark: true,
        Window: Color.FromArgb(38, 38, 38),
        Panel: Color.FromArgb(45, 45, 45),
        PanelAlt: Color.FromArgb(56, 56, 56),
        Border: Color.FromArgb(76, 76, 76),
        Text: Color.White,
        SubText: Color.FromArgb(205, 205, 205),
        Button: Color.FromArgb(72, 72, 72),
        ButtonText: Color.White,
        Primary: Color.FromArgb(28, 93, 166),
        PrimaryText: Color.White,
        Card: Color.FromArgb(67, 67, 67),
        CardText: Color.White,
        CardSubText: Color.FromArgb(215, 215, 215),
        InactiveText: Color.FromArgb(185, 185, 185),
        Grid: Color.FromArgb(35, 35, 35),
        GridHeader: Color.FromArgb(48, 48, 48),
        Selection: Color.FromArgb(28, 93, 166));
}
