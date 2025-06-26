; SyncGuard V3 Setup Script for Inno Setup
; 생성일: 2024년 1월
; 버전: 3.0.0

#define MyAppName "SyncGuard V3"
#define MyAppVersion "3.0.0"
#define MyAppPublisher "SyncGuard Team"
#define MyAppURL "https://github.com/syncguard"
#define MyAppExeName "SyncGuard.Tray.exe"

[Setup]
; 기본 설정
AppId={{12345678-1234-1234-1234-123456789012}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\SyncGuard
DefaultGroupName=SyncGuard
AllowNoIcons=yes
OutputDir=..\Output
OutputBaseFilename=SyncGuard_V3_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

; 한국어 지원
LanguageDetectionMethod=locale
ShowLanguageDialog=no

[Languages]
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "autostart"; Description: "{cm:AutoStartProgram,{#MyAppName}}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 메인 실행 파일
Source: "..\build\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\SyncGuard.Core.dll"; DestDir: "{app}"; Flags: ignoreversion

; 의존성 라이브러리들
Source: "..\build\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; 설정 파일 (기본값)
Source: "config\syncguard_config.txt"; DestDir: "{app}\config"; Flags: ignoreversion

; .NET 6.0 런타임 오프라인 설치 파일 포함
Source: "windowsdesktop-runtime-6.0.36-win-x64.exe"; DestDir: "{tmp}"; Flags: ignoreversion

; 로그 폴더 생성
[Dirs]
Name: "{app}\logs"; Permissions: users-full
Name: "{app}\config"; Permissions: users-full

[Icons]
; 시작 메뉴 바로가기
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"

; 바탕화면 바로가기
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; 빠른 실행 바로가기
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

; 자동 시작 등록
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: autostart

[Registry]
; 설치 정보 등록
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "DisplayName"; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "UninstallString"; ValueData: "{uninstallexe}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "DisplayIcon"; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "Publisher"; ValueData: "{#MyAppPublisher}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "URLInfoAbout"; ValueData: "{#MyAppURL}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "DisplayVersion"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey

[Run]
; 기존 프로그램 실행
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// 설치 전 확인사항
function InitializeSetup(): Boolean;
begin
  Result := True;
  // .NET 6.0 확인 제거 - 자동으로 설치됨
end;

// 설치 완료 후 .NET 런타임 설치
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  DotNetInstalled: Boolean;
begin
  if CurStep = ssPostInstall then
  begin
    Log('=== .NET 6.0 Runtime Installation Check ===');
    
    // 설치 전 .NET 런타임 상태 확인
    DotNetInstalled := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Core\Setup\InstalledVersions\x64\sharedhost');
    if DotNetInstalled then
      Log('Before installation - .NET 6.0 Runtime installed: True')
    else
      Log('Before installation - .NET 6.0 Runtime installed: False');
    
    if not DotNetInstalled then
    begin
      Log('Starting .NET 6.0 Runtime installation...');
      
      // 설치 파일 존재 확인
      if FileExists(ExpandConstant('{tmp}\windowsdesktop-runtime-6.0.36-win-x64.exe')) then
      begin
        Log('Found windowsdesktop-runtime-6.0.36-win-x64.exe in temp directory');
        Log('Executing: ' + ExpandConstant('{tmp}\windowsdesktop-runtime-6.0.36-win-x64.exe') + ' /install /quiet /norestart /log "{tmp}\dotnet_install.log"');
        
        // .NET 런타임 설치 실행
        if Exec(ExpandConstant('{tmp}\windowsdesktop-runtime-6.0.36-win-x64.exe'), '/install /quiet /norestart /log "{tmp}\dotnet_install.log"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
        begin
          Log('Installation process completed. Exit code: ' + IntToStr(ResultCode));
          if ResultCode = 0 then
          begin
            Log('Successfully installed .NET 6.0 Runtime');
          end
          else
          begin
            Log('Failed to install .NET 6.0 Runtime. Error code: ' + IntToStr(ResultCode));
          end;
        end
        else
        begin
          Log('Failed to start .NET 6.0 Runtime installation process');
        end;
      end
      else
      begin
        Log('ERROR: windowsdesktop-runtime-6.0.36-win-x64.exe not found in temp directory');
        Log('Temp directory: ' + ExpandConstant('{tmp}'));
      end;
    end
    else
    begin
      Log('.NET 6.0 Runtime is already installed - skipping installation');
    end;
    
    Log('=== .NET 6.0 Runtime Installation Check Complete ===');
  end;
end; 