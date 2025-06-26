# SyncGuard V3 프로젝트 완성 문서

## 📋 프로젝트 개요

**SyncGuard V3**는 NVIDIA Quadro Sync 상태를 실시간으로 모니터링하고 외부 서버로 전송하는 Windows 시스템 트레이 애플리케이션입니다.

### 🎯 주요 기능
- NVIDIA Quadro Sync 상태 실시간 감지 (Master/Slave/Error)
- 시스템 트레이 아이콘으로 시각적 상태 표시
- TCP 클라이언트를 통한 외부 서버 연결
- 설정 관리 및 저장 기능

---

## 🏗️ 프로젝트 구조

```
SyncGuard_V3/
├── SyncGuard.Core/           # 핵심 로직 라이브러리
│   ├── Class1.cs            # WMI 상태 감지, TCP 클라이언트
│   └── SyncGuard.Core.csproj
├── SyncGuard.Tray/          # 시스템 트레이 애플리케이션
│   ├── Form1.cs             # 메인 UI 및 로직
│   ├── Program.cs           # 진입점
│   └── SyncGuard.Tray.csproj
├── SyncGuard.sln            # 솔루션 파일
├── syncguard_log.txt        # 로그 파일
└── syncguard_config.txt     # 설정 파일
```

---

## 🔧 핵심 코드 분석

### 1. SyncGuard.Core/Class1.cs

#### 상태 열거형 정의
```csharp
public enum SyncStatus
{
    Unknown,
    Master,
    Slave,
    Error
}
```

#### WMI 상태 감지 로직
```csharp
private SyncStatus GetSyncStatusFromWmi()
{
    using (var searcher = new ManagementObjectSearcher("root\\CIMV2\\NV", "SELECT * FROM SyncTopology"))
    {
        var collection = searcher.Get();
        bool foundMaster = false;
        bool foundSlave = false;
        bool foundError = false;

        foreach (ManagementObject obj in collection)
        {
            int displaySyncState = Convert.ToInt32(obj["displaySyncState"]);
            int id = Convert.ToInt32(obj["id"]);
            bool isDisplayMasterable = Convert.ToBoolean(obj["isDisplayMasterable"]);
            
            if (displaySyncState == 2) foundMaster = true;
            else if (displaySyncState == 1) foundSlave = true;
            else if (displaySyncState == 0) foundError = true;
        }
        
        // 우선순위: Master > Slave > Error > Unknown
        if (foundMaster) return SyncStatus.Master;
        else if (foundSlave) return SyncStatus.Slave;
        else if (foundError) return SyncStatus.Error;
        else return SyncStatus.Unknown;
    }
}
```

#### TCP 클라이언트 기능
```csharp
public async Task SendStatusToServer()
{
    using var client = new TcpClient();
    await client.ConnectAsync(targetServerIp, targetServerPort);
    
    using var stream = client.GetStream();
    var status = GetExternalStatus(); // "192.168.0.201_state2"
    var message = status + "\r\n";
    var data = Encoding.UTF8.GetBytes(message);
    
    await stream.WriteAsync(data, 0, data.Length);
}
```

#### 외부 전송용 상태 변환
```csharp
private string GetExternalStatus()
{
    string localIp = GetLocalIpAddress(); // 현재 PC IP
    string status = lastStatus switch
    {
        SyncStatus.Master => "state2",
        SyncStatus.Slave => "state1",
        SyncStatus.Error => "state0",
        SyncStatus.Unknown => "state0",
        _ => "state0"
    };
    return $"{localIp}_{status}"; // "192.168.0.201_state2"
}
```

### 2. SyncGuard.Tray/Form1.cs

#### 메인 폼 초기화
```csharp
public Form1()
{
    InitializeComponent();
    LoadConfig(); // 설정 파일에서 값 불러오기
    
    this.WindowState = FormWindowState.Minimized;
    this.ShowInTaskbar = false;
    this.Visible = false;
    
    InitializeLogging();
    InitializeTrayIcon();
    
    syncChecker = new SyncChecker();
    StartTcpClient();
    syncChecker.SyncStatusChanged += OnSyncStatusChanged;
    
    InitializeSyncTimer();
}
```

#### 트레이 아이콘 색상 업데이트
```csharp
private void UpdateTrayIcon(SyncChecker.SyncStatus status)
{
    Color iconColor = status switch
    {
        SyncChecker.SyncStatus.Master => Color.Green,      // 초록색
        SyncChecker.SyncStatus.Slave => Color.Yellow,      // 노랑색
        SyncChecker.SyncStatus.Error => Color.Red,         // 빨간색
        SyncChecker.SyncStatus.Unknown => Color.Red,       // 빨간색
        _ => Color.Red
    };

    using (var bitmap = new Bitmap(16, 16))
    using (var graphics = Graphics.FromImage(bitmap))
    {
        graphics.Clear(iconColor);
        notifyIcon.Icon = Icon.FromHandle(bitmap.GetHicon());
    }
}
```

#### 설정 관리
```csharp
private void LoadConfig()
{
    if (File.Exists(configFilePath))
    {
        var lines = File.ReadAllLines(configFilePath);
        if (lines.Length >= 2)
        {
            targetIpAddress = lines[0].Trim();
            if (int.TryParse(lines[1].Trim(), out int port))
            {
                tcpServerPort = port;
            }
        }
    }
}

private void SaveConfig()
{
    var configData = new string[]
    {
        targetIpAddress,
        tcpServerPort.ToString()
    };
    File.WriteAllLines(configFilePath, configData);
}
```

#### 주기적 상태 체크 및 TCP 전송
```csharp
private void OnSyncTimerTick(object? sender, EventArgs e)
{
    var status = syncChecker.GetSyncStatus();
    UpdateTrayIcon(status);
    
    // 주기적으로 TCP 전송 (상태 변경 여부와 관계없이)
    if (isTcpClientEnabled && syncChecker != null)
    {
        _ = Task.Run(async () => 
        {
            await syncChecker.SendStatusToServer();
            LogMessage("DEBUG", "주기적 TCP 전송 완료");
        });
    }
}
```

---

## 📊 상태 매핑 테이블

| WMI displaySyncState | SyncStatus | 트레이 색상 | 전송 메시지 |
|---------------------|------------|-------------|-------------|
| 2 | Master | 초록색 | `192.168.0.201_state2` |
| 1 | Slave | 노랑색 | `192.168.0.201_state1` |
| 0 | Error | 빨간색 | `192.168.0.201_state0` |
| 기타 | Unknown | 빨간색 | `192.168.0.201_state0` |

---

## 🔄 동작 흐름

### 1. 초기화 과정
1. 설정 파일(`syncguard_config.txt`)에서 IP/포트 불러오기
2. WMI SyncTopology 클래스 초기화
3. 시스템 트레이 아이콘 생성
4. TCP 클라이언트 시작
5. 1초 타이머 시작

### 2. 상태 감지 과정
1. 1초마다 WMI 쿼리 실행
2. 모든 SyncTopology 디바이스 검사
3. 우선순위에 따라 상태 결정 (Master > Slave > Error)
4. 상태 변경 시 이벤트 발생

### 3. TCP 전송 과정
1. 현재 PC IP 주소 가져오기
2. 상태에 따른 메시지 생성 (`IP_state`)
3. TCP 서버에 연결
4. 메시지 전송 후 연결 종료

### 4. 설정 변경 과정
1. 트레이 아이콘 우클릭 → 설정
2. IP/포트 변경
3. 저장 버튼 클릭
4. 설정 파일에 저장
5. TCP 클라이언트 재시작

---

## 📁 파일 구조 및 용도

### 실행 파일
- `SyncGuard.Tray.exe`: 메인 실행 파일
- `SyncGuard.Core.dll`: 핵심 로직 라이브러리

### 설정 파일
- `syncguard_config.txt`: IP 주소 및 포트 설정
  ```
  192.168.0.100
  8080
  ```

### 로그 파일
- `syncguard_log.txt`: 상세 로그 기록
- `syncguard_log_YYYYMMDD_HHMMSS.txt`: 로그 백업 파일

---

## 🧪 테스트 결과

### ✅ 성공한 테스트
1. **WMI 상태 감지**: Master/Slave 상태 정확히 감지
2. **트레이 아이콘**: 상태에 따른 색상 변경 정상
3. **TCP 전송**: 로컬 서버 연결 및 메시지 전송 성공
4. **설정 저장**: IP/포트 변경 후 재시작 시 설정 유지
5. **다중 디스플레이**: 2개 디스플레이 환경에서 정상 작동

### 📝 로그 예시
```
[2025-06-26 17:04:09] [INFO] 설정 파일에서 불러옴: IP=192.168.0.150, Port=8080
[2025-06-26 17:04:09] [INFO] 디바이스 1001이 마스터 상태입니다 (State: 2)
[2025-06-26 17:04:09] [INFO] 마스터 디바이스가 발견되어 Master 상태로 설정합니다
[2025-06-26 17:04:09] [DEBUG] UpdateTrayIcon 호출됨 - 상태: Master
[2025-06-26 17:04:09] [DEBUG] 선택된 색상: Green
[2025-06-26 17:04:09] [INFO] TCP 클라이언트 시작: 192.168.0.150:8080
[2025-06-26 17:04:09] [INFO] TCP 서버 연결 성공
[2025-06-26 17:04:09] [INFO] 메시지 전송 시작: '192.168.0.201_state2' (22 bytes)
[2025-06-26 17:04:09] [INFO] 상태 전송 완료: 192.168.0.201_state2 -> 192.168.0.150:8080
[2025-06-26 17:04:09] [DEBUG] 주기적 TCP 전송 완료
```

---

## 🚀 배포 및 사용법

### 1. 빌드
```bash
dotnet build SyncGuard.sln
```

### 2. 실행
```bash
dotnet run --project SyncGuard.Tray
```

### 3. 설정 변경
1. 트레이 아이콘 우클릭
2. "설정" 선택
3. IP 주소 및 포트 변경
4. "저장" 버튼 클릭

### 4. 상태 확인
- **초록색**: Master 상태
- **노랑색**: Slave 상태  
- **빨간색**: Error/Unknown 상태

---

## 🛠️ 기술 스택

- **언어**: C# (.NET 9.0)
- **UI**: Windows Forms
- **시스템 통합**: WMI (Windows Management Instrumentation)
- **네트워크**: TCP Socket
- **로깅**: 파일 기반 로깅 시스템
- **설정**: 텍스트 파일 기반 설정 관리

---

## 📈 향후 확장 가능성

1. **웹 대시보드**: 실시간 상태 모니터링 웹 인터페이스
2. **알림 시스템**: 상태 변경 시 이메일/SMS 알림
3. **데이터베이스**: 상태 이력 저장 및 분석
4. **다중 프로토콜**: HTTP REST API, WebSocket 지원
5. **보안 강화**: SSL/TLS 암호화, 인증 시스템

---

## ✅ 프로젝트 완성도

**2단계 완성**: 기본 기능 + TCP 클라이언트 + 설정 관리

- ✅ WMI 기반 상태 감지
- ✅ 시스템 트레이 UI
- ✅ TCP 클라이언트
- ✅ 설정 저장/불러오기
- ✅ 로깅 시스템
- ✅ 다중 디스플레이 지원

**테스트 완료**: 로컬 환경에서 모든 기능 정상 작동 확인

---

## 📋 개발 이력

### 1단계: 기본 기능
- WMI를 통한 Quadro Sync 상태 감지
- 시스템 트레이 아이콘 UI 구현
- 기본 로깅 시스템

### 2단계: 네트워크 통신
- TCP 클라이언트 구현
- IP 주소 포함 상태 전송
- 설정 관리 및 저장 기능
- 다중 디스플레이 환경 지원

### 주요 해결 과제
1. **다중 디스플레이 문제**: 첫 번째 디바이스만 검사하던 문제를 모든 디바이스 검사로 해결
2. **TCP 연결 문제**: 설정 변경 후 재시작 기능 추가
3. **설정 유지 문제**: 설정 파일 저장/불러오기 기능 구현

---

*SyncGuard V3는 NVIDIA Quadro Sync 환경에서 실시간 모니터링을 위한 완전한 솔루션을 제공합니다.*

**개발 완료일**: 2025년 6월 26일  
**버전**: V3.0  
**상태**: 2단계 완성, 테스트 완료 