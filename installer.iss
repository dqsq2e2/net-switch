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
Source: "artifacts\lite\Net-Switch-Lite.exe"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion; Check: IsDotNet9DesktopInstalled
Source: "artifacts\publish\{#MyAppExeName}"; DestDir: "{app}"; DestName: "{#MyAppExeName}"; Flags: ignoreversion; Check: not IsDotNet9DesktopInstalled

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

[Code]
var
  DotNet9DesktopInstalled: Boolean;
  DotNetCheckCompleted: Boolean;

function IsDotNet9DesktopInstalled: Boolean;
var
  Versions: TArrayOfString;
  FindRec: TFindRec;
  I: Integer;
  RuntimePath: String;
begin
  if not DotNetCheckCompleted then
  begin
    DotNet9DesktopInstalled := False;
    if RegGetValueNames(
      HKLM64,
      'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
      Versions) then
    begin
      for I := 0 to GetArrayLength(Versions) - 1 do
      begin
        if Pos('9.', Versions[I]) = 1 then
        begin
          DotNet9DesktopInstalled := True;
          Break;
        end;
      end;
    end;

    if (not DotNet9DesktopInstalled) and RegGetValueNames(
      HKLM32,
      'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
      Versions) then
    begin
      for I := 0 to GetArrayLength(Versions) - 1 do
      begin
        if Pos('9.', Versions[I]) = 1 then
        begin
          DotNet9DesktopInstalled := True;
          Break;
        end;
      end;
    end;

    if not DotNet9DesktopInstalled then
    begin
      RuntimePath := ExpandConstant('{pf64}\dotnet\shared\Microsoft.WindowsDesktop.App');
      if FindFirst(RuntimePath + '\9.*', FindRec) then
      begin
        try
          repeat
            if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
            begin
              DotNet9DesktopInstalled := True;
              Break;
            end;
          until not FindNext(FindRec);
        finally
          FindClose(FindRec);
        end;
      end;
    end;

    DotNetCheckCompleted := True;
  end;
  Result := DotNet9DesktopInstalled;
end;

function GetRuntimeDescription(Param: String): String;
begin
  if IsDotNet9DesktopInstalled then
    Result := '已检测到 .NET 9 Desktop Runtime，将安装轻量版。'
  else
    Result := '未检测到 .NET 9 Desktop Runtime，将安装自带运行环境的完整版。';
end;

procedure InitializeWizard;
var
  RuntimeLabel: TNewStaticText;
begin
  RuntimeLabel := TNewStaticText.Create(WizardForm);
  RuntimeLabel.Parent := WizardForm.SelectDirPage;
  RuntimeLabel.Left := WizardForm.SelectDirLabel.Left;
  RuntimeLabel.Top := WizardForm.SelectDirBrowseLabel.Top + WizardForm.SelectDirBrowseLabel.Height + ScaleY(14);
  RuntimeLabel.Width := WizardForm.SelectDirPage.ClientWidth - ScaleX(40);
  RuntimeLabel.AutoSize := False;
  RuntimeLabel.WordWrap := True;
  RuntimeLabel.Height := ScaleY(40);
  RuntimeLabel.Caption := GetRuntimeDescription('');
end;
