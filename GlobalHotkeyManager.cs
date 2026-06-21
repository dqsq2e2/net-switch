using System.Runtime.InteropServices;

namespace NetAdapterSwitcher;

internal sealed class GlobalHotkeyManager
{
    private const int WmHotkey = 0x0312;
    private const int MainWindowId = 1101;
    private const int QuickPanelId = 1102;
    private readonly Form _window;

    public event Action? MainWindowRequested;
    public event Action? QuickPanelRequested;

    public GlobalHotkeyManager(Form window)
    {
        _window = window;
    }

    public void Register(HotkeySettings settings)
    {
        Unregister();
        uint modifiers = 0x4000;
        if (settings.Alt) modifiers |= 0x0001;
        if (settings.Control) modifiers |= 0x0002;
        if (settings.Shift) modifiers |= 0x0004;

        if (!RegisterHotKey(_window.Handle, MainWindowId, modifiers, (uint)settings.MainWindowKey))
            throw new InvalidOperationException($"快捷键 {settings.MainWindowText} 已被其他程序占用。");
        if (!RegisterHotKey(_window.Handle, QuickPanelId, modifiers, (uint)settings.QuickPanelKey))
        {
            UnregisterHotKey(_window.Handle, MainWindowId);
            throw new InvalidOperationException($"快捷键 {settings.QuickPanelText} 已被其他程序占用。");
        }
    }

    public void Unregister()
    {
        if (!_window.IsHandleCreated) return;
        UnregisterHotKey(_window.Handle, MainWindowId);
        UnregisterHotKey(_window.Handle, QuickPanelId);
    }

    public bool ProcessMessage(ref Message message)
    {
        if (message.Msg != WmHotkey) return false;
        int id = message.WParam.ToInt32();
        if (id == MainWindowId) MainWindowRequested?.Invoke();
        if (id == QuickPanelId) QuickPanelRequested?.Invoke();
        return true;
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
