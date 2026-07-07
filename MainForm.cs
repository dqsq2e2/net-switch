using System.Drawing.Drawing2D;
using System.Diagnostics;
using Microsoft.Win32;

namespace NetAdapterSwitcher;

internal sealed class MainForm : Form
{
    private const string StartupTaskName = "Net Switch Startup";
    private readonly PowerShellNetworkService _service = new();
    private readonly RouteMetricBackupStore _routeBackup = new();
    private readonly NotifyIcon _trayIcon = new();
    private readonly FlowLayoutPanel _adapterList = new();
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progress = new();
    private readonly ToolStripMenuItem _startupMenuItem = new("开机启动");
    private readonly System.Windows.Forms.Timer _backgroundRefreshTimer = new();
    private readonly HotkeySettings _hotkeySettings;
    private readonly GlobalHotkeyManager _hotkeyManager;
    private MainDashboardForm? _dashboard;
    private List<NetworkAdapterInfo> _cachedAdapters = [];
    private DateTime _lastRefresh;
    private bool _allowExit;
    private bool _busy;
    private readonly bool _startHidden;

    public MainForm(bool startHidden = false)
    {
        _startHidden = startHidden;
        RemoveLegacyStartupEntry();
        _hotkeySettings = HotkeySettings.Load();
        _hotkeyManager = new GlobalHotkeyManager(this);
        _hotkeyManager.MainWindowRequested += ShowDashboard;
        _hotkeyManager.QuickPanelRequested += ShowPopup;

        Text = "Net Switch";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = Color.FromArgb(38, 38, 38);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        ClientSize = new Size(448, 510);
        Padding = new Padding(1);

        BuildPopup();
        BuildTrayIcon();

        Shown += async (_, _) =>
        {
            Hide();
            await RefreshAdaptersAsync();
            if (!_startHidden)
                ShowDashboard();
        };
        Deactivate += (_, _) =>
        {
            if (!_busy) Hide();
        };
        FormClosing += OnFormClosing;
        HandleCreated += (_, _) => RegisterHotkeys(showError: true);

        _backgroundRefreshTimer.Interval = 5 * 60_000;
        _backgroundRefreshTimer.Tick += async (_, _) => await RefreshAdaptersAsync(true);
        _backgroundRefreshTimer.Start();
    }

    private void BuildPopup()
    {
        var border = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(76, 76, 76),
            Padding = new Padding(1)
        };
        var content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(45, 45, 45)
        };

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 82,
            BackColor = Color.FromArgb(45, 45, 45),
            Padding = new Padding(28, 18, 18, 8)
        };
        var title = new Label
        {
            AutoSize = true,
            Text = "选择主用网络",
            Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(28, 16)
        };
        var hint = new Label
        {
            AutoSize = true,
            Text = "点击网卡立即切换 · 总跃点越小越优先",
            ForeColor = Color.FromArgb(205, 205, 205),
            Location = new Point(30, 47)
        };
        var refresh = CreateIconButton("↻");
        refresh.Location = new Point(392, 21);
        refresh.Click += async (_, _) => await RefreshAdaptersAsync();
        header.Controls.AddRange([title, hint, refresh]);

        _adapterList.Dock = DockStyle.Fill;
        _adapterList.FlowDirection = FlowDirection.TopDown;
        _adapterList.WrapContents = false;
        _adapterList.AutoScroll = false;
        _adapterList.Padding = new Padding(24, 8, 24, 8);
        _adapterList.BackColor = Color.FromArgb(45, 45, 45);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 66,
            BackColor = Color.FromArgb(56, 56, 56),
            Padding = new Padding(24, 13, 24, 13)
        };
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Text = "后台运行中";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.AutoEllipsis = true;
        _statusLabel.ForeColor = Color.FromArgb(235, 235, 235);
        var restore = new Button
        {
            Text = "恢复自动",
            Dock = DockStyle.Right,
            Width = 104,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(72, 72, 72),
            ForeColor = Color.White,
            Cursor = Cursors.Hand
        };
        restore.FlatAppearance.BorderSize = 0;
        restore.Click += async (_, _) => await RestoreAllAsync();
        _progress.Dock = DockStyle.Bottom;
        _progress.Height = 3;
        _progress.Style = ProgressBarStyle.Marquee;
        _progress.Visible = false;
        footer.Controls.Add(_statusLabel);
        footer.Controls.Add(restore);
        footer.Controls.Add(_progress);

        content.Controls.Add(_adapterList);
        content.Controls.Add(footer);
        content.Controls.Add(header);
        border.Controls.Add(content);
        Controls.Add(border);
    }

    private void BuildTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("打开主界面", null, (_, _) => ShowDashboard());
        menu.Items.Add("打开快速面板", null, (_, _) => ShowPopup());
        menu.Items.Add("刷新", null, async (_, _) => await RefreshAdaptersAsync());
        menu.Items.Add("恢复全部自动跃点", null, async (_, _) => await RestoreAllAsync());
        menu.Items.Add("设置全局快捷键…", null, (_, _) => ConfigureHotkeys());
        menu.Items.Add(new ToolStripSeparator());
        _startupMenuItem.Checked = IsStartupEnabled();
        _startupMenuItem.Click += (_, _) => ToggleStartup();
        menu.Items.Add(_startupMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitApplication());

        _trayIcon.Icon = Icon is null ? SystemIcons.Application : (Icon)Icon.Clone();
        _trayIcon.Text = "Net Switch";
        _trayIcon.Visible = true;
        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowPopup();
        };
        _trayIcon.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowDashboard();
        };
    }

    private void ShowPopup()
    {
        if (Visible)
        {
            Hide();
            return;
        }

        PositionNearTaskbar();
        RenderAdapters(_cachedAdapters);
        Show();
        Activate();
    }

    private void ShowDashboard()
    {
        _dashboard ??= new MainDashboardForm(
            () => RefreshAdaptersAsync(),
            SetPreferredAsync,
            RestoreAllAsync,
            ConfigureHotkeys);
        _dashboard.UpdateAdapters(_cachedAdapters, CacheStatusText());
        _dashboard.UpdateHotkeyHint(_hotkeySettings);
        if (!_dashboard.Visible)
            _dashboard.Show();
        _dashboard.WindowState = FormWindowState.Normal;
        _dashboard.Activate();
    }

    private void PositionNearTaskbar()
    {
        Screen screen = Screen.FromPoint(Cursor.Position);
        Rectangle area = screen.WorkingArea;
        const int margin = 12;
        Location = new Point(area.Right - Width - margin, area.Bottom - Height - margin);
    }

    private async Task RefreshAdaptersAsync(bool silent = false)
    {
        if (silent)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                var adapters = await _service.GetAdaptersAsync();
                UpdateAdapterCache(adapters);
            }
            catch
            {
                // 后台刷新失败时保留上一次缓存，不打扰用户。
            }
            finally
            {
                _busy = false;
            }
            return;
        }

        await RunBusyAsync(silent ? _statusLabel.Text : "正在读取网络…", async () =>
        {
            var adapters = await _service.GetAdaptersAsync();
            UpdateAdapterCache(adapters);
        });
    }

    private void UpdateAdapterCache(IReadOnlyList<NetworkAdapterInfo> adapters)
    {
        _cachedAdapters = adapters
            .OrderByDescending(a => a.IsConnected)
            .ThenByDescending(a => a.HasDefaultRoute)
            .ThenBy(a => a.EffectiveMetric)
            .ToList();
        _lastRefresh = DateTime.Now;
        RenderAdapters(_cachedAdapters);
        _statusLabel.Text = adapters.Count == 0
            ? "没有找到物理以太网或 WLAN"
            : $"已找到 {adapters.Count} 张网卡";
        _dashboard?.UpdateAdapters(_cachedAdapters, CacheStatusText());
    }

    private void RenderAdapters(IReadOnlyList<NetworkAdapterInfo> adapters)
    {
        _adapterList.SuspendLayout();
        _adapterList.Controls.Clear();
        foreach (var adapter in adapters)
            _adapterList.Controls.Add(CreateAdapterCard(adapter));
        _adapterList.ResumeLayout();
        _dashboard?.UpdateAdapters(adapters, CacheStatusText());
    }

    private Control CreateAdapterCard(NetworkAdapterInfo adapter)
    {
        bool connected = adapter.IsConnected;
        var card = new Panel
        {
            Width = 382,
            Height = 112,
            Margin = new Padding(0, 0, 0, 10),
            BackColor = adapter.HasDefaultRoute
                ? Color.FromArgb(28, 93, 166)
                : Color.FromArgb(67, 67, 67),
            Cursor = connected ? Cursors.Hand : Cursors.Default,
            Tag = adapter
        };
        card.Region = new Region(RoundedRectangle(new Rectangle(Point.Empty, card.Size), 8));
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRectangle(card.ClientRectangle, 8);
            using var brush = new SolidBrush(card.BackColor);
            e.Graphics.FillPath(brush, path);
        };

        var icon = new Label
        {
            Text = adapter.NetworkType == "WLAN" ? "◉" : "▣",
            Font = new Font("Segoe UI Symbol", 17F),
            ForeColor = connected ? Color.White : Color.FromArgb(170, 170, 170),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(14, 32),
            Size = new Size(38, 42)
        };
        var name = new Label
        {
            Text = adapter.Name,
            AutoEllipsis = true,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(62, 12),
            Size = new Size(235, 22)
        };
        var details = new Label
        {
            Text = connected ? $"IP：{EmptyAsDash(adapter.IpAddress)}" : "未连接",
            AutoEllipsis = true,
            ForeColor = connected ? Color.FromArgb(232, 232, 232) : Color.FromArgb(185, 185, 185),
            Location = new Point(62, 37),
            Size = new Size(290, 21)
        };
        var gateway = new Label
        {
            Text = $"网关：{EmptyAsDash(adapter.Gateway)}",
            AutoSize = false,
            AutoEllipsis = false,
            ForeColor = Color.FromArgb(215, 215, 215),
            Location = new Point(62, 58),
            Size = new Size(290, 21)
        };
        var speed = new Label
        {
            Text = $"速率：{EmptyAsDash(adapter.LinkSpeed)}",
            AutoSize = false,
            ForeColor = Color.FromArgb(215, 215, 215),
            Location = new Point(222, 84),
            Size = new Size(130, 21),
            TextAlign = ContentAlignment.TopRight
        };
        var metric = new Label
        {
            Text = adapter.HasDefaultRoute ? $"总跃点 {adapter.EffectiveMetric}" : "无默认路由",
            AutoSize = true,
            ForeColor = adapter.HasDefaultRoute
                ? Color.White
                : Color.FromArgb(190, 190, 190),
            Location = new Point(62, 84)
        };
        var active = new Label
        {
            Text = adapter.HasDefaultRoute && adapter.EffectiveMetric <= 5 ? "当前" : "›",
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight,
            Location = new Point(312, 12),
            Size = new Size(48, 24)
        };

        card.Controls.AddRange([icon, name, details, gateway, metric, speed, active]);
        if (connected)
            AttachClick(card, async () => await SetPreferredAsync(adapter));
        return card;
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        Rectangle r = new(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        path.AddArc(r.Left, r.Top, diameter, diameter, 180, 90);
        path.AddArc(r.Right - diameter, r.Top, diameter, diameter, 270, 90);
        path.AddArc(r.Right - diameter, r.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(r.Left, r.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void AttachClick(Control control, Func<Task> action)
    {
        control.Click += async (_, _) => await action();
        foreach (Control child in control.Controls)
        {
            child.Cursor = Cursors.Hand;
            child.Click += async (_, _) => await action();
        }
    }

    private async Task SetPreferredAsync(NetworkAdapterInfo adapter)
    {
        await RunBusyAsync($"正在切换到 {adapter.Name}…", async () =>
        {
            await _routeBackup.SaveMissingAsync(await _service.GetDefaultRoutesAsync());
            await _service.SetPreferredAsync(adapter.InterfaceIndex);
            var adapters = await _service.GetAdaptersAsync();
            _cachedAdapters = adapters
                .OrderByDescending(a => a.IsConnected)
                .ThenByDescending(a => a.HasDefaultRoute)
                .ThenBy(a => a.EffectiveMetric)
                .ToList();
            _lastRefresh = DateTime.Now;
            RenderAdapters(_cachedAdapters);
            _statusLabel.Text = $"已切换到 {adapter.Name}";
            _trayIcon.ShowBalloonTip(1600, "网卡切换完成",
                $"{adapter.Name} 已设为主用，其他默认路由已降低优先级。",
                ToolTipIcon.Info);
        });
    }

    private async Task RestoreAllAsync()
    {
        await RunBusyAsync("正在恢复自动跃点…", async () =>
        {
            await _service.RestoreAutomaticMetricAsync();
            var routes = await _routeBackup.GetAsync();
            await _service.RestoreRouteMetricsAsync(routes);
            await _routeBackup.RemoveAsync();
            await RefreshAdaptersCoreAsync();
            _statusLabel.Text = "已恢复 Windows 自动跃点";
        });
    }

    private async Task RefreshAdaptersCoreAsync()
    {
        var adapters = await _service.GetAdaptersAsync();
        _cachedAdapters = adapters
            .OrderByDescending(a => a.IsConnected)
            .ThenByDescending(a => a.HasDefaultRoute)
            .ThenBy(a => a.EffectiveMetric)
            .ToList();
        _lastRefresh = DateTime.Now;
        RenderAdapters(_cachedAdapters);
    }

    private async Task RunBusyAsync(string message, Func<Task> action)
    {
        if (_busy) return;
        _busy = true;
        _statusLabel.Text = message;
        _progress.Visible = true;
        UseWaitCursor = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "操作失败";
            _trayIcon.ShowBalloonTip(2500, "网卡切换失败", ex.Message, ToolTipIcon.Error);
            if (Visible)
                MessageBox.Show(this, ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _busy = false;
            _progress.Visible = false;
            UseWaitCursor = false;
        }
    }

    private static string EmptyAsDash(string value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    private string CacheStatusText() =>
        _lastRefresh == default
            ? "正在读取网络配置…"
            : $"后台运行中 · 最近刷新 {_lastRefresh:HH:mm:ss}";

    private static Button CreateIconButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(34, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(241, 243, 246),
            ForeColor = Color.White,
            Font = new Font("Segoe UI Symbol", 13F),
            Cursor = Cursors.Hand
        };
        button.BackColor = Color.FromArgb(62, 62, 62);
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static bool IsStartupEnabled()
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"),
            Arguments = $"/Query /TN \"{StartupTaskName}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });
        process?.WaitForExit();
        return process?.ExitCode == 0;
    }

    private static void RemoveLegacyStartupEntry()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        key?.DeleteValue("NetAdapterSwitcher", throwOnMissingValue: false);
    }

    private void ToggleStartup()
    {
        try
        {
            if (_startupMenuItem.Checked)
            {
                RunScheduledTaskCommand("/Delete", "/TN", StartupTaskName, "/F");
                _startupMenuItem.Checked = false;
                _trayIcon.ShowBalloonTip(1600, "开机启动", "已关闭开机启动。", ToolTipIcon.Info);
            }
            else
            {
                string action = $"\"{Application.ExecutablePath}\" --startup";
                RunScheduledTaskCommand(
                    "/Create", "/TN", StartupTaskName,
                    "/TR", action,
                    "/SC", "ONLOGON",
                    "/RL", "HIGHEST",
                    "/F");
                _startupMenuItem.Checked = true;
                _trayIcon.ShowBalloonTip(1800, "开机启动",
                    "已创建登录计划任务，下次登录后将在托盘后台启动。",
                    ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            _startupMenuItem.Checked = IsStartupEnabled();
            MessageBox.Show(_dashboard ?? (IWin32Window)this, ex.Message, "开机启动设置失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void RunScheduledTaskCommand(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("无法启动 Windows 计划任务工具。");
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error) ? output.Trim() : error.Trim());
    }

    private void ConfigureHotkeys()
    {
        using var dialog = new HotkeySettingsForm(_hotkeySettings);
        Form owner = _dashboard is { Visible: true } ? _dashboard : this;
        if (dialog.ShowDialog(owner) != DialogResult.OK) return;

        var old = new HotkeySettings
        {
            MainWindowKey = _hotkeySettings.MainWindowKey,
            QuickPanelKey = _hotkeySettings.QuickPanelKey,
            Control = _hotkeySettings.Control,
            Alt = _hotkeySettings.Alt,
            Shift = _hotkeySettings.Shift
        };
        CopyHotkeys(dialog.Settings, _hotkeySettings);
        try
        {
            _hotkeyManager.Register(_hotkeySettings);
            _hotkeySettings.Save();
            _dashboard?.UpdateHotkeyHint(_hotkeySettings);
            _trayIcon.ShowBalloonTip(1800, "快捷键已更新",
                $"主界面：{_hotkeySettings.MainWindowText}\n快速面板：{_hotkeySettings.QuickPanelText}",
                ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            CopyHotkeys(old, _hotkeySettings);
            RegisterHotkeys(showError: false);
            MessageBox.Show(owner, ex.Message, "快捷键设置失败",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RegisterHotkeys(bool showError)
    {
        try
        {
            _hotkeyManager.Register(_hotkeySettings);
        }
        catch (Exception ex)
        {
            if (showError)
                _trayIcon.ShowBalloonTip(2500, "快捷键注册失败", ex.Message, ToolTipIcon.Warning);
        }
    }

    private static void CopyHotkeys(HotkeySettings source, HotkeySettings target)
    {
        target.MainWindowKey = source.MainWindowKey;
        target.QuickPanelKey = source.QuickPanelKey;
        target.Control = source.Control;
        target.Alt = source.Alt;
        target.Shift = source.Shift;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_allowExit)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void ExitApplication()
    {
        _allowExit = true;
        _backgroundRefreshTimer.Stop();
        _hotkeyManager.Unregister();
        if (_dashboard is not null)
            _dashboard.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var path = RoundedRectangle(ClientRectangle, 10);
        Region = new Region(path);
    }

    protected override void WndProc(ref Message m)
    {
        if (_hotkeyManager?.ProcessMessage(ref m) == true)
            return;
        base.WndProc(ref m);
    }
}
