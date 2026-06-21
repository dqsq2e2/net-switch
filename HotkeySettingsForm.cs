namespace NetAdapterSwitcher;

internal sealed class HotkeySettingsForm : Form
{
    private readonly ComboBox _mainKey = new();
    private readonly ComboBox _quickKey = new();
    private readonly CheckBox _control = new();
    private readonly CheckBox _alt = new();
    private readonly CheckBox _shift = new();

    public HotkeySettings Settings { get; }

    public HotkeySettingsForm(HotkeySettings settings)
    {
        Settings = new HotkeySettings
        {
            MainWindowKey = settings.MainWindowKey,
            QuickPanelKey = settings.QuickPanelKey,
            Control = settings.Control,
            Alt = settings.Alt,
            Shift = settings.Shift
        };

        Text = "设置全局快捷键";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(390, 245);
        Font = new Font("Microsoft YaHei UI", 9F);

        var keys = Enumerable.Range((int)Keys.A, 26)
            .Select(value => (Keys)value)
            .Concat(Enumerable.Range((int)Keys.F1, 12).Select(value => (Keys)value))
            .Cast<object>()
            .ToArray();
        _mainKey.Items.AddRange(keys);
        _quickKey.Items.AddRange(keys);
        _mainKey.SelectedItem = Settings.MainWindowKey;
        _quickKey.SelectedItem = Settings.QuickPanelKey;
        _mainKey.DropDownStyle = ComboBoxStyle.DropDownList;
        _quickKey.DropDownStyle = ComboBoxStyle.DropDownList;

        _control.Text = "Ctrl";
        _alt.Text = "Alt";
        _shift.Text = "Shift";
        _control.Checked = Settings.Control;
        _alt.Checked = Settings.Alt;
        _shift.Checked = Settings.Shift;

        Controls.Add(MakeLabel("组合键", 24, 25));
        _control.SetBounds(112, 20, 65, 28);
        _alt.SetBounds(181, 20, 58, 28);
        _shift.SetBounds(244, 20, 70, 28);
        Controls.AddRange([_control, _alt, _shift]);

        Controls.Add(MakeLabel("打开主界面", 24, 82));
        _mainKey.SetBounds(150, 77, 190, 30);
        Controls.Add(_mainKey);
        Controls.Add(MakeLabel("打开快速面板", 24, 127));
        _quickKey.SetBounds(150, 122, 190, 30);
        Controls.Add(_quickKey);

        var save = new Button
        {
            Text = "保存",
            DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(38, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        save.FlatAppearance.BorderSize = 0;
        save.SetBounds(246, 186, 94, 34);
        save.Click += (_, e) =>
        {
            if (!_control.Checked && !_alt.Checked && !_shift.Checked)
            {
                MessageBox.Show(this, "请至少选择 Ctrl、Alt 或 Shift 中的一个修饰键。", "快捷键", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }
            if (_mainKey.SelectedItem is not Keys main || _quickKey.SelectedItem is not Keys quick || main == quick)
            {
                MessageBox.Show(this, "两组快捷键必须选择不同的按键。", "快捷键", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }
            Settings.MainWindowKey = main;
            Settings.QuickPanelKey = quick;
            Settings.Control = _control.Checked;
            Settings.Alt = _alt.Checked;
            Settings.Shift = _shift.Checked;
        };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel };
        cancel.SetBounds(142, 186, 94, 34);
        Controls.AddRange([cancel, save]);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private static Label MakeLabel(string text, int x, int y) =>
        new() { Text = text, AutoSize = true, Location = new Point(x, y) };
}
