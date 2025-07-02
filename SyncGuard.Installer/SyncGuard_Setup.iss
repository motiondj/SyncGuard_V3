; SyncGuard V3 Setup Script for Inno Setup
; 생성일: 2024년 1월
; 버전: 3.0.0
; 인코딩: UTF-8 with BOM

#define MyAppName "SyncGuard V3"
#define MyAppVersion "3.0.0"
#define MyAppPublisher "SyncGuard Team"
#define MyAppURL "https://github.com/syncguard"
#define MyAppExeName "SyncGuard.Tray.exe"

[Code]
function GetInstallDate(Param: string): string;
begin
  Result := GetDateTimeString('yyyymmdd', '-', ':');
end;

procedure InitializeWizard();
begin
  // 설치 마법사 초기화
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  DefaultConfigPath: String;
  UserConfigPath: String;
  UserConfigDir: String;
begin
  if CurStep = ssPostInstall then
  begin
    // 사용자 설정 폴더 경로
    UserConfigDir := ExpandConstant('{userappdata}\SyncGuard');
    UserConfigPath := UserConfigDir + '\syncguard_config.json';
    DefaultConfigPath := ExpandConstant('{app}\defaults\default_config.json');
    
    // 설정 폴더가 없으면 생성
    if not DirExists(UserConfigDir) then
      CreateDir(UserConfigDir);
    
    // 기존 설정 파일이 없는 경우에만 기본 설정 복사
    if not FileExists(UserConfigPath) then
    begin
      FileCopy(DefaultConfigPath, UserConfigPath, False);
    end;
  end;
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  // 사용자 데이터 삭제 여부 확인
  if MsgBox('프로그램 설정과 로그 파일을 삭제하시겠습니까?' + #13#10 + 
            '(아니오를 선택하면 설정이 보존됩니다)', 
            mbConfirmation, MB_YESNO) = IDYES then
  begin
    // 사용자 데이터 삭제
    DelTree(ExpandConstant('{userappdata}\SyncGuard'), True, True, True);
    DelTree(ExpandConstant('{localappdata}\SyncGuard'), True, True, True);
  end;
end;

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
SetupIconFile=..\Docs\icon\icon.ico

; Uninstall 설정
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
UninstallFilesDir={app}\uninstall

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
; Self-contained 실행 파일 (모든 의존성 포함)
Source: "..\SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\self-contained\SyncGuard.Tray.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\self-contained\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\self-contained\*.json"; DestDir: "{app}"; Flags: ignoreversion

; 기본 설정 파일 (사용자 폴더에 복사)
Source: "config\default_config.json"; DestDir: "{app}\defaults"; Flags: ignoreversion

; 사용자 데이터 폴더 생성
[Dirs]
Name: "{userappdata}\SyncGuard"; Permissions: users-full
Name: "{localappdata}\SyncGuard\logs"; Permissions: users-full

[Icons]
; 시작 메뉴 바로가기
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\SyncGuard V3 제거"; Filename: "{uninstallexe}"

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
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "InstallLocation"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: dword; ValueName: "NoModify"; ValueData: 1; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: dword; ValueName: "NoRepair"; ValueData: 1; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "EstimatedSize"; ValueData: "50000"; Flags: uninsdeletekey
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppName}"; ValueType: string; ValueName: "InstallDate"; ValueData: "{code:GetInstallDate}"; Flags: uninsdeletekey

[UninstallDelete]
; 프로그램 폴더의 남은 파일들 삭제
Type: filesandordirs; Name: "{app}" 