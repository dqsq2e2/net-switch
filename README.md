# Net Switch

Net Switch 是一个轻量的 Windows 多网卡优先级切换工具，通过 Windows 原生接口跃点和路由跃点控制默认网络优先级，不会直接禁用其他网卡。

## 功能

- 仅显示物理以太网和 WLAN
- 一键切换主用网卡，同时降低其他默认路由优先级
- 完整显示 IPv4、默认网关、连接速率和跃点
- 主界面、托盘快速面板和全局快捷键
- 后台缓存与每 5 分钟静默刷新
- 恢复 Windows 自动接口跃点及原始路由跃点
- 可选开机启动

默认快捷键：

- `Ctrl + Alt + N`：打开主界面
- `Ctrl + Alt + Q`：打开快速面板

## 下载

从仓库的 [Releases](https://github.com/dqsq2e2/net-switch/releases) 下载：

- `Net-Switch-Setup.exe`：安装包
- `Net Switch.exe`：免安装单文件版

程序需要管理员权限修改网络跃点，启动时会显示 Windows UAC 提示。

## 本地构建

需要 .NET 9 SDK：

```powershell
.\build.ps1
```

输出文件位于 `artifacts\publish\Net Switch.exe`。

安装包使用 Inno Setup 6：

```powershell
$env:APP_VERSION = "1.0.0"
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" .\installer.iss
```

## 自动发布

推送 `v*` 标签时，GitHub Actions 会：

1. 构建 Windows x64 自包含单文件程序
2. 生成 Inno Setup 安装包
3. 创建 GitHub Release
4. 上传安装包和免安装 EXE

例如：

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## 工作原理

Windows 会综合“接口跃点 + 路由跃点”选择总跃点更低的默认路由。Net Switch 将选中网卡的接口跃点设为 5、默认路由跃点设为 0，并降低其他默认路由的优先级。原始路由跃点会被备份，以便恢复。

VPN 的强制路由、策略路由或过滤驱动可能覆盖普通跃点规则，此时应优先使用 VPN 客户端自身的分流设置。
