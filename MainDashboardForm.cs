namespace NetAdapterSwitcher;

internal sealed class MainDashboardForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly Label _status = new();
    private readonly Func<Task> _refresh;
    private readonly Func<NetworkAdapterInfo, Task> _switch;
    private readonly Func<Task> _restore;
    private readonly Action _configureHotkeys;
    private readonly Label _hotkeyHint = new();
    private readonly Panel _header = new();
    private readonly Label _title = new();
    private readonly Label _subtitle = new();
    private readonly FlowLayoutPanel _toolbar = new();
    private readonly Panel _gridHost = new();
    private readonly List<Button> _buttons = [];
    private List<NetworkAdapterInfo> _adapters = [];

    public MainDashboardForm(
        Func<Task> refresh,
        Func<NetworkAdapterInfo, Task> switchAdapter,
        Func<Task> restore,
        Action configureHotkeys)
    {
        _refresh = refresh;
        _switch = switchAdapter;
        _restore = restore;
        _configureHotkeys = configureHotkeys;

        Text = "Net Switch";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(880, 500);
        Rectangle workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Size = new Size(
            Math.Min(1240, workArea.Width - 80),
            Math.Min(680, workArea.Height - 80));
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildLayout();
        ApplyTheme();
        HandleCreated += (_, _) => ThemeService.ApplyWindowTheme(this);
        FormClosing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }

    public void UpdateAdapters(IReadOnlyList<NetworkAdapterInfo> adapters, string status)
    {
        _adapters = adapters.ToList();
        _grid.DataSource = null;
        _grid.DataSource = _adapters;
        _status.Text = status;
    }

    public void UpdateHotkeyHint(HotkeySettings settings) =>
        _hotkeyHint.Text = $"主界面：{settings.MainWindowText}　快速面板：{settings.QuickPanelText}";

    private void BuildLayout()
    {
        _header.Dock = DockStyle.Top;
        _header.Height = 92;
        _header.Padding = new Padding(26, 15, 20, 10);
        _title.Text = "Net Switch";
        _title.AutoSize = true;
        _title.ForeColor = Color.White;
        _title.Font = new Font(Font.FontFamily, 17F, FontStyle.Bold);
        _title.Location = new Point(25, 14);
        _subtitle.Text = "选择物理以太网或 WLAN，自动调整默认路由优先级";
        _subtitle.AutoSize = true;
        _subtitle.Location = new Point(27, 55);
        _header.Controls.AddRange([_title, _subtitle]);

        _toolbar.Dock = DockStyle.Top;
        _toolbar.Height = 62;
        _toolbar.Padding = new Padding(20, 13, 10, 8);
        var switchButton = MakeButton("设为主用", true, 110);
        var refreshButton = MakeButton("刷新", false, 82);
        var restoreButton = MakeButton("恢复自动跃点", false, 130);
        var hotkeyButton = MakeButton("设置快捷键", false, 112);
        _buttons.AddRange([switchButton, refreshButton, restoreButton, hotkeyButton]);
        switchButton.Click += async (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is NetworkAdapterInfo adapter)
                await _switch(adapter);
        };
        refreshButton.Click += async (_, _) => await _refresh();
        restoreButton.Click += async (_, _) => await _restore();
        hotkeyButton.Click += (_, _) => _configureHotkeys();
        _hotkeyHint.AutoSize = true;
        _hotkeyHint.Margin = new Padding(10, 9, 0, 0);
        _toolbar.Controls.AddRange([switchButton, refreshButton, restoreButton, hotkeyButton, _hotkeyHint]);

        ConfigureGrid();
        _gridHost.Dock = DockStyle.Fill;
        _gridHost.Padding = new Padding(20, 16, 20, 12);
        _gridHost.Controls.Add(_grid);

        _status.Dock = DockStyle.Bottom;
        _status.Height = 40;
        _status.Padding = new Padding(22, 0, 0, 0);
        _status.TextAlign = ContentAlignment.MiddleLeft;

        Controls.Add(_gridHost);
        Controls.Add(_status);
        Controls.Add(_toolbar);
        Controls.Add(_header);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.ReadOnly = true;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowHeadersVisible = false;
        _grid.BorderStyle = BorderStyle.None;
        _grid.BackgroundColor = Color.White;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.GridColor = Color.FromArgb(229, 233, 239);
        _grid.RowTemplate.Height = 48;
        _grid.ColumnHeadersHeight = 42;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);

        AddColumn("Name", "网卡", 130);
        AddColumn("StatusText", "状态", 70);
        AddColumn("IpAddress", "IPv4 地址", 150);
        AddColumn("Gateway", "默认网关", 135);
        AddColumn("LinkSpeed", "连接速率", 95);
        AddColumn("InterfaceMetric", "接口", 65);
        AddColumn("RouteMetric", "路由", 65);
        AddColumn("PriorityText", "总跃点", 68);
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Description",
            HeaderText = "设备",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 210
        });
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is NetworkAdapterInfo adapter)
                await _switch(adapter);
        };
    }

    private void AddColumn(string property, string title, int width) =>
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = property,
            HeaderText = title,
            Width = width
        });

    private static Button MakeButton(string text, bool primary, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 34,
            Margin = new Padding(0, 0, 10, 0),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(38, 99, 235) : Color.FromArgb(234, 238, 243),
            ForeColor = primary ? Color.White : Color.FromArgb(42, 51, 64),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 1;
        return button;
    }

    public void ApplyTheme()
    {
        AppTheme theme = ThemeService.Current;
        ThemeService.ApplyWindowTheme(this);
        BackColor = theme.Window;
        _header.BackColor = theme.IsDark ? theme.PanelAlt : Color.FromArgb(28, 39, 56);
        _title.ForeColor = Color.White;
        _subtitle.ForeColor = theme.IsDark ? theme.SubText : Color.FromArgb(188, 199, 214);
        _toolbar.BackColor = theme.Panel;
        _gridHost.BackColor = theme.Window;
        _status.BackColor = theme.Panel;
        _status.ForeColor = theme.SubText;
        _hotkeyHint.ForeColor = theme.SubText;

        foreach (Button button in _buttons)
        {
            bool primary = button.Text == "设为主用";
            button.BackColor = primary ? theme.Primary : theme.Button;
            button.ForeColor = primary ? theme.PrimaryText : theme.ButtonText;
            button.FlatAppearance.BorderColor = primary ? theme.PrimaryBorder : theme.ButtonBorder;
            button.FlatAppearance.MouseOverBackColor = primary ? theme.PrimaryHover : theme.ButtonHover;
            button.FlatAppearance.MouseDownBackColor = primary ? theme.PrimaryHover : theme.ButtonHover;
        }

        _grid.BackgroundColor = theme.Grid;
        _grid.GridColor = theme.IsDark ? Color.FromArgb(65, 65, 65) : Color.FromArgb(229, 233, 239);
        _grid.ColumnHeadersDefaultCellStyle.BackColor = theme.GridHeader;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = theme.Text;
        _grid.DefaultCellStyle.BackColor = theme.Grid;
        _grid.DefaultCellStyle.ForeColor = theme.Text;
        _grid.DefaultCellStyle.SelectionBackColor = theme.Selection;
        _grid.DefaultCellStyle.SelectionForeColor = theme.PrimaryText;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = theme.IsDark
            ? Color.FromArgb(40, 40, 40)
            : Color.White;
        _grid.EnableHeadersVisualStyles = false;
    }
}
