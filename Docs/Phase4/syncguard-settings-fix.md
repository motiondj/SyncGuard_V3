# SyncGuard 설정 파일 저장 문제 해결 가이드

## 1. ConfigManager 클래스 수정

**파일**: `SyncGuard.Core/Class1.cs`

### 수정할 부분 찾기:
`ConfigManager` 클래스에서 아래 부분들을 찾아서 수정합니다.

### 1-1. 설정 디렉토리 경로 변경

**현재 코드** (약 1045번째 줄):
```csharp
private static readonly string configDirectory = Path.Combine(GetApplicationDirectory(), "config");
private static readonly string configFile = Path.Combine(configDirectory, "syncguard_config.txt");
```

**수정 코드**:
```csharp
// %APPDATA%\SyncGuard 폴더 사용
private static readonly string configDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
    "SyncGuard");
private static readonly string configFile = Path.Combine(configDirectory, "syncguard_config.json");
```

### 1-2. GetApplicationDirectory 메서드 제거 또는 수정

이 메서드는 더 이상 설정 파일 경로에 사용되지 않으므로, 로그 파일 경로용으로만 사용하거나 제거합니다.

## 2. Logger 클래스 수정

**파일**: `SyncGuard.Core/Class1.cs`

### 2-1. 로그 디렉토리 경로 변경

**현재 코드** (약 940번째 줄):
```csharp
private static readonly string logDirectory = Path.Combine(GetApplicationDirectory(), "logs");
```

**수정 코드**:
```csharp
// %LOCALAPPDATA%\SyncGuard\logs 폴더 사용
private static readonly string logDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
    "SyncGuard", 
    "logs");
```

### 2-2. GetApplicationDirectory 메서드 수정

**현재 코드**:
```csharp
private static string GetApplicationDirectory()
{
    try
    {
        return AppContext.BaseDirectory;
    }
    catch
    {
        return ".";
    }
}
```

**수정 코드**:
```csharp
private static string GetApplicationDirectory()
{
    // 더 이상 사용하지 않음 - 필요시 제거
    return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
}
```

## 3. Form1.cs 파일은 수정 불필요

`Form1.cs`에서는 이미 `ConfigManager.LoadConfig()`와 `ConfigManager.SaveConfig()`를 사용하고 있으므로 별도 수정이 필요없습니다.

## 4. 프로젝트 설정 파일 수정

**파일**: `SyncGuard.Tray/SyncGuard.Tray.csproj`

### 4-1. PublishSingleFile 제거

**현재 내용**:
```xml
<PublishSingleFile>true</PublishSingleFile>
<SelfContained>true</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

**수정 내용**:
```xml
<!-- PublishSingleFile 제거 -->
<!-- <PublishSingleFile>true</PublishSingleFile> -->
<SelfContained>false</SelfContained>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

## 5. Inno Setup 스크립트 수정

**파일**: `SyncGuard.Installer/SyncGuard_Setup.iss`

### 5-1. 파일 섹션 수정

**현재 내용**:
```iss
[Files]
Source: "..\SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\publish\SyncGuard.Tray.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "config\syncguard_config.txt"; DestDir: "{app}\config"; Flags: ignoreversion
```

**수정 내용**:
```iss
[Files]
; 실행 파일 및 DLL 파일들
Source: "..\SyncGuard.Tray\bin\Release\net6.0-windows\SyncGuard.Tray.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SyncGuard.Tray\bin\Release\net6.0-windows\SyncGuard.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SyncGuard.Tray\bin\Release\net6.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; 기본 설정 파일 (사용자 폴더에 복사)
Source: "config\default_config.json"; DestDir: "{app}\defaults"; Flags: ignoreversion
```

### 5-2. 디렉토리 섹션 수정

**현재 내용**:
```iss
[Dirs]
Name: "{app}\logs"; Permissions: users-full
Name: "{app}\config"; Permissions: users-full
```

**수정 내용**:
```iss
[Dirs]
; 사용자 데이터 폴더 생성
Name: "{userappdata}\SyncGuard"; Permissions: users-full
Name: "{localappdata}\SyncGuard\logs"; Permissions: users-full
```

### 5-3. 코드 섹션 추가 (초기 설정 파일 복사)

**추가할 내용**:
```iss
[Code]
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
```

## 6. 기본 설정 파일 생성

**새 파일**: `SyncGuard.Installer/config/default_config.json`

```json
{
  "ServerIP": "127.0.0.1",
  "ServerPort": 8080,
  "TransmissionInterval": 1000,
  "EnableExternalSend": false,
  "LastUpdated": "2024-01-01 00:00:00"
}
```

## 7. 빌드 및 패키징 순서

### 7-1. 프로젝트 빌드
```bash
# Release 모드로 빌드
dotnet build -c Release
```

### 7-2. Inno Setup으로 설치 파일 생성
1. Inno Setup Compiler 실행
2. `SyncGuard_Setup.iss` 파일 열기
3. Compile (Ctrl+F9)

## 8. 테스트 확인 사항

### 설치 후 확인:
1. `C:\Program Files\SyncGuard\` - 프로그램 파일들
2. `%APPDATA%\SyncGuard\` - 설정 파일 (syncguard_config.json)
3. `%LOCALAPPDATA%\SyncGuard\logs\` - 로그 파일들

### 동작 확인:
1. 프로그램 실행 → 설정 변경 → 프로그램 종료
2. 프로그램 재실행 → 변경된 설정이 유지되는지 확인

## 9. 문제 해결

### 권한 오류 발생 시:
- 설정 파일 경로가 `%APPDATA%`인지 확인
- Program Files 폴더에 쓰기를 시도하지 않는지 확인

### 설정이 저장되지 않을 때:
- 로그 파일에서 "설정 저장 완료" 메시지 확인
- `%APPDATA%\SyncGuard\syncguard_config.json` 파일 존재 확인

## 10. 추가 개선사항

### 10-1. ConfigManager 클래스에 에러 처리 강화

**파일**: `SyncGuard.Core/Class1.cs`

**SaveConfig 메서드 수정** (약 1100번째 줄):
```csharp
public static void SaveConfig(string serverIP, int serverPort, int transmissionInterval = 1000, bool enableExternalSend = false)
{
    lock (lockObject)
    {
        try
        {
            Logger.Info($"설정 저장 시작 - configDirectory: {configDirectory}");
            Logger.Info($"설정 저장 시작 - configFile: {configFile}");
            
            // 설정 디렉토리가 없으면 생성
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
                Logger.Info($"설정 디렉토리 생성: {configDirectory}");
            }
            
            // 기존 설정 파일 백업
            if (File.Exists(configFile))
            {
                string backupFile = configFile + ".bak";
                File.Copy(configFile, backupFile, true);
                Logger.Info($"기존 설정 파일 백업: {backupFile}");
            }
            
            var config = new
            {
                ServerIP = serverIP,
                ServerPort = serverPort,
                TransmissionInterval = transmissionInterval,
                EnableExternalSend = enableExternalSend,
                LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFile, json, System.Text.Encoding.UTF8);
            Logger.Info($"설정 저장 완료");
        }
        catch (Exception ex)
        {
            Logger.Error($"설정 저장 실패: {ex.Message}");
            throw; // 상위로 예외 전파
        }
    }
}
```

### 10-2. 레거시 설정 마이그레이션 개선

**TryMigrateLegacyConfig 메서드 수정**:
```csharp
private static bool TryMigrateLegacyConfig()
{
    try
    {
        // 여러 위치에서 레거시 설정 파일 확인
        string[] possibleLegacyPaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "syncguard_config.txt"),
            Path.Combine(AppContext.BaseDirectory, "config", "syncguard_config.txt"),
            Path.Combine(Environment.CurrentDirectory, "syncguard_config.txt"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "SyncGuard", "syncguard_config.txt"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SyncGuard", "syncguard_config.txt")
        };
        
        foreach (string legacyPath in possibleLegacyPaths)
        {
            if (File.Exists(legacyPath))
            {
                Logger.Info($"레거시 설정 파일 발견: {legacyPath}");
                
                // 레거시 파일 읽기 및 마이그레이션 로직...
                // (기존 코드 유지)
                
                return true;
            }
        }
    }
    catch (Exception ex)
    {
        Logger.Error($"레거시 설정 마이그레이션 실패: {ex.Message}");
    }
    
    return false;
}
```

### 10-3. Form1.cs에 첫 실행 감지 추가

**파일**: `SyncGuard.Tray/Form1.cs`

**InitializeComponent() 이후에 추가**:
```csharp
// 첫 실행 확인
CheckFirstRun();

private void CheckFirstRun()
{
    string firstRunFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SyncGuard",
        "first_run_complete.txt"
    );
    
    if (!File.Exists(firstRunFile))
    {
        // 첫 실행
        ShowToastNotification("SyncGuard에 오신 것을 환영합니다!", 
            "설정 메뉴에서 TCP 서버를 구성해주세요.");
        
        // 첫 실행 완료 표시
        try
        {
            var dir = Path.GetDirectoryName(firstRunFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            
            File.WriteAllText(firstRunFile, DateTime.Now.ToString());
        }
        catch { }
    }
}
```

### 10-4. Inno Setup 언인스톨 개선

**파일**: `SyncGuard.Installer/SyncGuard_Setup.iss`

**추가할 섹션**:
```iss
[Code]
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

[UninstallDelete]
; 프로그램 폴더의 남은 파일들 삭제
Type: filesandordirs; Name: "{app}"
```

### 10-5. 설정 파일 유효성 검사 추가

**ConfigManager.LoadConfig 메서드에 추가**:
```csharp
// JSON 파싱 후 유효성 검사
if (string.IsNullOrWhiteSpace(serverIP) || !IPAddress.TryParse(serverIP, out _))
{
    Logger.Warning($"잘못된 IP 주소: {serverIP}, 기본값 사용");
    serverIP = "127.0.0.1";
}

if (serverPort < 1 || serverPort > 65535)
{
    Logger.Warning($"잘못된 포트 번호: {serverPort}, 기본값 사용");
    serverPort = 8080;
}

if (transmissionInterval < 100 || transmissionInterval > 3600000) // 0.1초 ~ 1시간
{
    Logger.Warning($"잘못된 전송 간격: {transmissionInterval}ms, 기본값 사용");
    transmissionInterval = 1000;
}
```

## 11. 주의사항

1. **기존 사용자 마이그레이션**: 위의 개선된 마이그레이션 코드가 자동으로 처리

2. **언인스톨 시**: 사용자에게 설정 삭제 여부를 묻는 대화상자 표시

3. **.NET Runtime**: `SelfContained`를 `false`로 변경했으므로, 대상 PC에 .NET 6.0 Runtime 필요

4. **백업 파일**: 설정 저장 시 자동으로 `.bak` 파일 생성

5. **권한 문제**: 모든 사용자 데이터는 %APPDATA%와 %LOCALAPPDATA%에 저장되므로 권한 문제 없음