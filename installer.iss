#define MyAppName "Net Switch"
#define MyAppVersion GetEnv("APP_VERSION")
#if MyAppVersion == ""
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "dqsq2e2"
#define MyAppExeName "Net Switch.exe"

[Setup]
AppId={{7C54EE97-28BA-42C4-A946-C1665D3673D7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Net Switch
DefaultGroupName=Net Switch
DisableProgramGroupPage=yes
OutputDir=artifacts\installer
OutputBaseFilename=Net-Switch-Setup
SetupIconFile=net-switch.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "artifacts\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Net Switch"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Net Switch"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项："; Flags: unchecked
Name: "startup"; Description: "开机启动 Net Switch"; GroupDescription: "附加选项："; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NetAdapterSwitcher"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 Net Switch"; Flags: nowait postinstall skipifsilent
