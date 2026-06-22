namespace NetAdapterSwitcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\NetSwitch", out bool isFirstInstance);
        if (!isFirstInstance)
            return;

        ApplicationConfiguration.Initialize();
        bool startHidden = Environment.GetCommandLineArgs()
            .Any(argument => argument.Equals("--startup", StringComparison.OrdinalIgnoreCase));
        Application.Run(new MainForm(startHidden));
    }
}
