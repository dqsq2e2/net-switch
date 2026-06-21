using System.Text.Json;

namespace NetAdapterSwitcher;

internal sealed class HotkeySettings
{
    public Keys MainWindowKey { get; set; } = Keys.N;
    public Keys QuickPanelKey { get; set; } = Keys.Q;
    public bool Control { get; set; } = true;
    public bool Alt { get; set; } = true;
    public bool Shift { get; set; }

    public string MainWindowText => Format(MainWindowKey);
    public string QuickPanelText => Format(QuickPanelKey);

    public static HotkeySettings Load()
    {
        string path = SettingsPath();
        if (!File.Exists(path)) return new HotkeySettings();
        try
        {
            return JsonSerializer.Deserialize<HotkeySettings>(File.ReadAllText(path)) ?? new HotkeySettings();
        }
        catch
        {
            return new HotkeySettings();
        }
    }

    public void Save()
    {
        string path = SettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }

    private string Format(Keys key)
    {
        var parts = new List<string>();
        if (Control) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        parts.Add(key.ToString());
        return string.Join(" + ", parts);
    }

    private static string SettingsPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NetAdapterSwitcher",
        "hotkeys.json");
}
