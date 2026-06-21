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
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(880, 500);
        Rectangle workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Size = new Size(
            Math.Min(1240, workArea.Width - 80),
            Math.Min(680, workArea.Height - 80));
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(245, 247, 250);

        BuildLayout();
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
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 92,
            BackColor = Color.FromArgb(28, 39, 56),
            Padding = new Padding(26, 15, 20, 10)
        };
        header.Controls.Add(new Label
        {
            Text = "Net Switch",
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font(Font.FontFamily, 17F, FontStyle.Bold),
            Location = new Point(25, 14)
        });
        header.Controls.Add(new Label
        {
            Text = "选择物理以太网或 WLAN，自动调整默认路由优先级",
            AutoSize = true,
            ForeColor = Color.FromArgb(188, 199, 214),
            Location = new Point(27, 55)
        });

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 62,
            Padding = new Padding(20, 13, 10, 8),
            BackColor = Color.White
        };
        var switchButton = MakeButton("设为主用", true, 110);
        var refreshButton = MakeButton("刷新", false, 82);
        var restoreButton = MakeButton("恢复自动跃点", false, 130);
        var hotkeyButton = MakeButton("设置快捷键", false, 112);
        switchButton.Click += async (_, _) =>
        {
            if (_grid.CurrentRow?.DataBoundItem is NetworkAdapterInfo adapter)
                await _switch(adapter);
        };
        refreshButton.Click += async (_, _) => await _refresh();
        restoreButton.Click += async (_, _) => await _restore();
        hotkeyButton.Click += (_, _) => _configureHotkeys();
        _hotkeyHint.AutoSize = true;
        _hotkeyHint.ForeColor = Color.FromArgb(100, 108, 120);
        _hotkeyHint.Margin = new Padding(10, 9, 0, 0);
        toolbar.Controls.AddRange([switchButton, refreshButton, restoreButton, hotkeyButton, _hotkeyHint]);

        ConfigureGrid();
        var gridHost = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 12)
        };
        gridHost.Controls.Add(_grid);

        _status.Dock = DockStyle.Bottom;
        _status.Height = 40;
        _status.Padding = new Padding(22, 0, 0, 0);
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.BackColor = Color.White;
        _status.ForeColor = Color.FromArgb(85, 94, 108);

        Controls.Add(gridHost);
        Controls.Add(_status);
        Controls.Add(toolbar);
        Controls.Add(header);
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
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 251);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(226, 237, 255);
        _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(28, 42, 65);

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
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}
