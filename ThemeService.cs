using Microsoft.Win32;
using System.Runtime.InteropServices;

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
    Color ButtonHover,
    Color ButtonBorder,
    Color ButtonText,
    Color Primary,
    Color PrimaryHover,
    Color PrimaryBorder,
    Color PrimaryText,
    Color Card,
    Color CardHover,
    Color CardBorder,
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

    public static void ApplyWindowTheme(Form form, bool useTransientBackdrop = false)
    {
        if (!form.IsHandleCreated || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            return;

        AppTheme theme = Current;
        int darkMode = theme.IsDark ? 1 : 0;
        int cornerPreference = 2;
        int backdropType = useTransientBackdrop ? 3 : 2;
        int borderColor = ToColorRef(theme.Border);

        DwmSetWindowAttribute(form.Handle, 20, ref darkMode, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 33, ref cornerPreference, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 34, ref borderColor, sizeof(int));
        DwmSetWindowAttribute(form.Handle, 38, ref backdropType, sizeof(int));
        SetWindowTheme(form.Handle, theme.IsDark ? "DarkMode_Explorer" : "Explorer", null);
    }

    private static int ToColorRef(Color color) =>
        color.R | (color.G << 8) | (color.B << 16);

    public static readonly AppTheme Light = new(
        IsDark: false,
        Window: Color.FromArgb(243, 243, 243),
        Panel: Color.FromArgb(243, 243, 243),
        PanelAlt: Color.FromArgb(249, 249, 249),
        Border: Color.FromArgb(205, 205, 205),
        Text: Color.FromArgb(27, 27, 27),
        SubText: Color.FromArgb(92, 92, 92),
        Button: Color.FromArgb(251, 251, 251),
        ButtonHover: Color.FromArgb(246, 246, 246),
        ButtonBorder: Color.FromArgb(229, 229, 229),
        ButtonText: Color.FromArgb(32, 32, 32),
        Primary: Color.FromArgb(0, 95, 184),
        PrimaryHover: Color.FromArgb(0, 85, 166),
        PrimaryBorder: Color.FromArgb(0, 85, 166),
        PrimaryText: Color.White,
        Card: Color.FromArgb(251, 251, 251),
        CardHover: Color.FromArgb(247, 247, 247),
        CardBorder: Color.FromArgb(229, 229, 229),
        CardText: Color.FromArgb(28, 28, 28),
        CardSubText: Color.FromArgb(78, 78, 78),
        InactiveText: Color.FromArgb(112, 112, 112),
        Grid: Color.FromArgb(251, 251, 251),
        GridHeader: Color.FromArgb(243, 243, 243),
        Selection: Color.FromArgb(0, 103, 192));

    public static readonly AppTheme Dark = new(
        IsDark: true,
        Window: Color.FromArgb(32, 32, 32),
        Panel: Color.FromArgb(32, 32, 32),
        PanelAlt: Color.FromArgb(39, 39, 39),
        Border: Color.FromArgb(72, 72, 72),
        Text: Color.FromArgb(255, 255, 255),
        SubText: Color.FromArgb(200, 200, 200),
        Button: Color.FromArgb(51, 51, 51),
        ButtonHover: Color.FromArgb(59, 59, 59),
        ButtonBorder: Color.FromArgb(69, 69, 69),
        ButtonText: Color.FromArgb(255, 255, 255),
        Primary: Color.FromArgb(96, 205, 255),
        PrimaryHover: Color.FromArgb(106, 211, 255),
        PrimaryBorder: Color.FromArgb(96, 205, 255),
        PrimaryText: Color.FromArgb(0, 44, 61),
        Card: Color.FromArgb(51, 51, 51),
        CardHover: Color.FromArgb(58, 58, 58),
        CardBorder: Color.FromArgb(69, 69, 69),
        CardText: Color.White,
        CardSubText: Color.FromArgb(215, 215, 215),
        InactiveText: Color.FromArgb(185, 185, 185),
        Grid: Color.FromArgb(35, 35, 35),
        GridHeader: Color.FromArgb(48, 48, 48),
        Selection: Color.FromArgb(28, 93, 166));

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr windowHandle, string? subAppName, string? subIdList);
}
