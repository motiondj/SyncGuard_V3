# SyncGuard_V3 개발 계획

## 📋 프로젝트 개요

### 목표
NVIDIA Quadro Sync 카드를 사용하는 멀티 서버 환경에서 동기화 상태를 모니터링하는 독립적인 트레이 아이콘 애플리케이션 개발

### 핵심 요구사항
- **동기화 연결 상태 확인**: 데이지체인으로 연결된 Sync 카드들의 연결 상태
- **Master/Slave 관계 확인**: 어느 GPU가 Master이고 어느 것이 Slave인지
- **Sync Lock 상태**: 실제로 동기화가 이루어지고 있는지 (Synced 상태)
- **트레이 아이콘 표시**: 간단한 상태 표시
- **토스트 알림**: 문제 발생 시에만 알림
- **설정 기능**: 모니터링 간격, 알림 설정 등

### 추가 고려사항
- **설정 마이그레이션**: 이전 버전(V1, V2) 설정 자동 가져오기
- **원격 모니터링 준비**: 다중 서버 중앙 모니터링 확장 가능 구조
- **진단 도구**: 시스템 정보 수집 및 리포트 생성
- **최소 권한 실행**: 관리자/일반 권한 기능 분리

### 시스템 환경
- **개발 환경**: RTX 5080 (로컬 개발)
- **테스트 환경**: 실제 Quadro Sync 서버들 (별도)
- **GPU**: NVIDIA RTX 6000 Ada Generation (실제 환경)
- **동기화**: Quadro Sync 카드 (데이지체인 연결)
- **OS**: Windows (WDDM 모드)
- **관련 SW**: 언리얼엔진 nDisplay (Switchboard 미사용)

---

## 🏗️ 기술 아키텍처

### 개발 언어: C#/.NET
**선택 이유:**
- NVAPI 바인딩 가능 (NvAPIWrapper 라이브러리)
- 트레이 아이콘 구현 용이 (System.Windows.Forms.NotifyIcon)
- Windows 네이티브 기능 활용 (Performance Counters, WMI)
- 개발 속도와 성능의 균형

### 프로젝트 구조
```
SyncGuard_V3/
├── SyncGuard.Core/          # 핵심 Sync 모니터링 로직
│   ├── NVAPI/              # NVAPI 래퍼 클래스들
│   ├── Monitoring/         # Sync 상태 모니터링
│   ├── Models/             # 데이터 모델
│   └── Diagnostics/        # 진단 도구
├── SyncGuard.Tray/          # 트레이 아이콘 애플리케이션
│   ├── Forms/              # UI 폼들
│   └── Services/           # 트레이 서비스
├── SyncGuard.Settings/      # 설정 관리
│   ├── Forms/              # 설정 UI
│   ├── Configuration/      # 설정 저장/로드
│   └── Migration/          # 설정 마이그레이션
├── SyncGuard.Common/        # 공통 유틸리티
│   ├── Logging/            # 로깅 시스템
│   ├── Notifications/      # 알림 시스템
│   ├── Extensions/         # 확장 메서드들
│   └── Security/           # 권한 관리
├── SyncGuard.Remote/        # 원격 모니터링 (미래 확장)
│   ├── Client/             # 원격 클라이언트
│   ├── Server/             # 중앙 서버
│   └── Communication/      # 통신 프로토콜
└── SyncGuard.Installer/     # 설치 프로그램
```

---

## 🔍 구현 방법 (nDisplay/Switchboard 분석 기반)

### 1. nDisplay 방식 활용 (우선순위 1)

#### NvAPI_D3D1x_QueryFrameCount 사용
```csharp
// nDisplay의 NvidiaSyncWatchdog 방식
public class QuadroSyncMonitor
{
    private IntPtr d3dDevice;
    
    public bool CheckSyncStatus()
    {
        uint frameCount = 0;
        NvAPI_Status result = NvAPI_D3D1x_QueryFrameCount(d3dDevice, ref frameCount);
        
        if (result == NvAPI_Status.NVAPI_OK)
        {
            // 프레임 카운터가 정상적으로 증가하는지 확인
            return ValidateFrameCounter(frameCount);
        }
        return false;
    }
}
```

#### Present Barrier 통계 수집
```csharp
// nDisplay의 Present Barrier 방식
public class PresentBarrierMonitor
{
    public SyncStatistics GetSyncStatistics()
    {
        NV_PRESENT_BARRIER_FRAME_STATISTICS stats = new();
        stats.dwVersion = NV_PRESENT_BARRIER_FRAME_STATICS_VER1;
        
        NvAPI_Status result = NvAPI_QueryPresentBarrierFrameStatistics(
            barrierClientHandle, ref stats);
            
        if (result == NvAPI_Status.NVAPI_OK)
        {
            return new SyncStatistics
            {
                PresentCount = stats.PresentCount,
                PresentInSyncCount = stats.PresentInSyncCount,
                FlipInSyncCount = stats.FlipInSyncCount,
                SyncMode = stats.SyncMode
            };
        }
        return null;
    }
}
```

### 2. Switchboard 방식 활용 (우선순위 2)

#### Sync 토폴로지 정보 수집
```csharp
// Switchboard의 syncTopos 방식
public class SyncTopologyMonitor
{
    public SyncTopologyInfo GetSyncTopology()
    {
        // NVAPI를 통해 Sync 토폴로지 정보 수집
        var syncTopos = QuerySyncTopology();
        
        return new SyncTopologyInfo
        {
            GpuSyncStatus = syncTopos.Select(topo => 
                topo.syncGpus.Select(gpu => gpu.bIsSynced).ToArray()).ToArray(),
            HouseSyncStatus = syncTopos.Select(topo => 
                topo.syncStatusParams.bHouseSync).ToArray(),
            SyncSource = syncTopos.Select(topo => 
                topo.syncControlParams.source).ToArray()
        };
    }
}
```

### 3. 대안 방법들 (문서 기반)

#### Windows Performance Counters
```csharp
public class PerformanceCounterMonitor
{
    public float GetFrameLockStatus()
    {
        using var counter = new PerformanceCounter(
            "NVIDIA GPU", "Frame Lock Status", "GPU 0");
        return counter.NextValue(); // 0 = Unlocked, 1 = Locked
    }
}
```

#### nvidia-smi XML 파싱
```csharp
public class NvidiaSMIMonitor
{
    public SyncInfo GetSyncInfo()
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "nvidia-smi",
            Arguments = "-q -x",
            RedirectStandardOutput = true,
            UseShellExecute = false
        });
        
        var xmlOutput = process.StandardOutput.ReadToEnd();
        return ParseSyncXML(xmlOutput);
    }
}
```

---

## 🎯 개발 단계

### Phase 1: 기본 기능 (1-2주)
1. ✅ 프로젝트 구조 생성
2. ✅ 트레이 아이콘 표시
3. ✅ Sync 상태 확인 (기본)
4. ✅ 컨텍스트 메뉴
5. ✅ 설정 창

### Phase 2: 고급 기능 (2-3주)
1. 🔄 NVAPI 직접 호출 구현
2. 🔄 대안 방법들 구현
3. 🔄 로깅 시스템
4. 🔄 알림 시스템
5. 🔄 설정 저장/로드

### Phase 3: 확장 기능 (2-3주)
1. 🔄 설정 마이그레이션 구현
2. 🔄 진단 도구 개발
3. 🔄 권한 관리 시스템
4. 🔄 원격 모니터링 기반 구조

### Phase 4: 최적화 및 테스트 (1-2주)
1. 🔄 성능 최적화
2. 🔄 에러 처리 강화
3. 🔄 실제 환경 테스트
4. 🔄 배포 준비

---

## 🛠️ 기술 스택

### 필수 패키지
```xml
<!-- SyncGuard.Core -->
<PackageReference Include="NvAPIWrapper" Version="1.0.0" />
<PackageReference Include="System.Management" Version="7.0.0" />

<!-- SyncGuard.Tray -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.4.0" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.22621.2428" />

<!-- SyncGuard.Common -->
<PackageReference Include="Serilog" Version="3.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />

<!-- SyncGuard.Remote (미래 확장) -->
<PackageReference Include="SignalR.Client" Version="8.0.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

### 개발 도구
- **IDE**: Visual Studio 2022
- **.NET**: .NET 8.0
- **빌드**: MSBuild
- **버전 관리**: Git

---

## 📊 Sync 상태 판단 로직

### SyncStatus 클래스
```csharp
public class SyncStatus
{
    public bool IsHardwareSyncEnabled { get; set; }
    public bool IsFrameLockActive { get; set; }
    public bool IsHouseSyncConnected { get; set; }
    public string SyncSource { get; set; } // "Vsync", "House"
    public float SyncQuality { get; set; } // 0.0 ~ 1.0
    public List<string> Issues { get; set; } = new();
    public string ErrorMessage { get; set; }
    
    public bool HasIssues => Issues.Count > 0;
    public bool IsValid => string.IsNullOrEmpty(ErrorMessage);
}
```

### 상태 판단 우선순위
1. **NVAPI 직접 호출** (nDisplay 방식)
2. **Present Barrier 통계** (nDisplay 방식)
3. **Sync 토폴로지** (Switchboard 방식)
4. **Performance Counters** (문서 방식)
5. **nvidia-smi** (문서 방식)

---

## 🔧 추가 고려사항 상세 구현

### 1. 설정 마이그레이션

#### 마이그레이션 매니저
```csharp
public class SettingsMigrationManager
{
    public async Task<bool> MigrateFromPreviousVersion()
    {
        try
        {
            // V1 설정 확인 및 마이그레이션
            if (await CheckV1Settings())
            {
                await MigrateV1ToV3();
            }
            
            // V2 설정 확인 및 마이그레이션
            if (await CheckV2Settings())
            {
                await MigrateV2ToV3();
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"설정 마이그레이션 실패: {ex.Message}");
            return false;
        }
    }
    
    private async Task MigrateV1ToV3()
    {
        // V1 설정 파일 위치: %APPDATA%\SyncGuard\config.ini
        var v1ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SyncGuard", "config.ini");
            
        if (File.Exists(v1ConfigPath))
        {
            var v1Settings = ParseV1Config(v1ConfigPath);
            var v3Settings = ConvertV1ToV3(v1Settings);
            await SaveV3Settings(v3Settings);
        }
    }
}
```

### 2. 원격 모니터링 준비

#### 확장 가능한 구조
```csharp
public interface IMonitoringService
{
    Task<SyncStatus> GetLocalSyncStatus();
    Task<bool> SendStatusToRemoteServer(SyncStatus status);
    Task<List<RemoteServerStatus>> GetRemoteServersStatus();
}

public class LocalMonitoringService : IMonitoringService
{
    public async Task<SyncStatus> GetLocalSyncStatus()
    {
        // 로컬 Sync 상태 확인
        return await Task.Run(() => syncMonitor.GetSyncStatus());
    }
    
    public async Task<bool> SendStatusToRemoteServer(SyncStatus status)
    {
        // 향후 원격 서버로 상태 전송
        // SignalR 또는 gRPC 사용 예정
        return await Task.FromResult(true);
    }
}
```

#### 원격 통신 프로토콜 (미래 확장)
```csharp
public class RemoteCommunicationProtocol
{
    // SignalR Hub 클라이언트
    private HubConnection hubConnection;
    
    public async Task InitializeConnection(string serverUrl)
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(serverUrl)
            .Build();
            
        await hubConnection.StartAsync();
    }
    
    public async Task SendSyncStatus(SyncStatus status)
    {
        await hubConnection.InvokeAsync("SendSyncStatus", status);
    }
}
```

### 3. 진단 도구

#### 진단 리포트 생성
```csharp
public class DiagnosticReport
{
    public async Task<string> GenerateReport()
    {
        var report = new StringBuilder();
        
        // 시스템 정보 수집
        report.AppendLine("=== 시스템 정보 ===");
        report.AppendLine($"OS: {Environment.OSVersion}");
        report.AppendLine($"Machine: {Environment.MachineName}");
        report.AppendLine($"User: {Environment.UserName}");
        
        // GPU 정보
        report.AppendLine("\n=== GPU 정보 ===");
        var gpuInfo = await CollectGPUInfo();
        report.AppendLine(gpuInfo);
        
        // 사용 가능한 모니터링 방법 체크
        report.AppendLine("\n=== 모니터링 방법 체크 ===");
        var monitoringMethods = await CheckAvailableMonitoringMethods();
        report.AppendLine(monitoringMethods);
        
        // 최근 로그 포함
        report.AppendLine("\n=== 최근 로그 ===");
        var recentLogs = await GetRecentLogs();
        report.AppendLine(recentLogs);
        
        // Sync 상태
        report.AppendLine("\n=== 현재 Sync 상태 ===");
        var syncStatus = await GetCurrentSyncStatus();
        report.AppendLine(syncStatus.ToString());
        
        return report.ToString();
    }
    
    private async Task<string> CollectGPUInfo()
    {
        var gpuInfo = new StringBuilder();
        
        try
        {
            var gpus = GPUApi.GetGPUs();
            foreach (var gpu in gpus)
            {
                gpuInfo.AppendLine($"GPU: {gpu.Name}");
                gpuInfo.AppendLine($"Driver: {gpu.DriverVersion}");
                gpuInfo.AppendLine($"Memory: {gpu.MemoryInfo.TotalMemory} MB");
            }
        }
        catch (Exception ex)
        {
            gpuInfo.AppendLine($"GPU 정보 수집 실패: {ex.Message}");
        }
        
        return gpuInfo.ToString();
    }
    
    private async Task<string> CheckAvailableMonitoringMethods()
    {
        var methods = new StringBuilder();
        
        // NVAPI 체크
        try
        {
            NvAPI_Initialize();
            methods.AppendLine("✅ NVAPI 사용 가능");
        }
        catch
        {
            methods.AppendLine("❌ NVAPI 사용 불가");
        }
        
        // Performance Counters 체크
        try
        {
            using var counter = new PerformanceCounter("NVIDIA GPU", "Frame Lock Status", "GPU 0");
            methods.AppendLine("✅ Performance Counters 사용 가능");
        }
        catch
        {
            methods.AppendLine("❌ Performance Counters 사용 불가");
        }
        
        return methods.ToString();
    }
}
```

### 4. 최소 권한 실행

#### 권한 관리 시스템
```csharp
public class PermissionManager
{
    public enum PermissionLevel
    {
        Normal,     // 일반 권한
        Elevated    // 관리자 권한
    }
    
    public PermissionLevel CurrentPermissionLevel { get; private set; }
    
    public PermissionManager()
    {
        CurrentPermissionLevel = CheckCurrentPermissionLevel();
    }
    
    private PermissionLevel CheckCurrentPermissionLevel()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator) 
                ? PermissionLevel.Elevated 
                : PermissionLevel.Normal;
        }
        catch
        {
            return PermissionLevel.Normal;
        }
    }
    
    public bool CanAccessNVAPI()
    {
        // NVAPI는 일반 권한으로도 접근 가능
        return true;
    }
    
    public bool CanAccessPerformanceCounters()
    {
        // Performance Counters는 일반 권한으로도 접근 가능
        return true;
    }
    
    public bool CanModifyRegistry()
    {
        // 레지스트리 수정은 관리자 권한 필요
        return CurrentPermissionLevel == PermissionLevel.Elevated;
    }
    
    public bool CanInstallService()
    {
        // 서비스 설치는 관리자 권한 필요
        return CurrentPermissionLevel == PermissionLevel.Elevated;
    }
}
```

#### 기능별 권한 분리
```csharp
public class SyncGuardApplication
{
    private PermissionManager permissionManager;
    
    public SyncGuardApplication()
    {
        permissionManager = new PermissionManager();
        InitializeFeatures();
    }
    
    private void InitializeFeatures()
    {
        // 일반 권한으로 가능한 기능들
        InitializeBasicMonitoring();
        InitializeTrayIcon();
        InitializeLogging();
        
        // 관리자 권한이 필요한 기능들
        if (permissionManager.CurrentPermissionLevel == PermissionLevel.Elevated)
        {
            InitializeAdvancedFeatures();
            InitializeServiceInstallation();
        }
        else
        {
            ShowElevationRequiredMessage();
        }
    }
    
    private void ShowElevationRequiredMessage()
    {
        var message = "일부 고급 기능을 사용하려면 관리자 권한이 필요합니다.\n" +
                     "기본 모니터링 기능은 정상적으로 작동합니다.";
        
        MessageBox.Show(message, "권한 안내", 
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
```

---

## 🎨 UI/UX 설계

### 트레이 아이콘 상태
- **🟢 녹색**: 동기화 정상 (IsHardwareSyncEnabled && IsFrameLockActive)
- **🟡 노란색**: 하드웨어 준비됨 (IsHardwareSyncEnabled && !IsFrameLockActive)
- **🔴 빨간색**: 동기화 비활성 (!IsHardwareSyncEnabled)

### 컨텍스트 메뉴
```
┌─────────────────────────┐
│ Sync Status: Active     │
│ ─────────────────────── │
│ Settings                │
│ Refresh                 │
│ Diagnostics             │
│ ─────────────────────── │
│ Exit                    │
└─────────────────────────┘
```

### 설정 창
```
┌─────────────────────────────────────┐
│ SyncGuard Settings                  │
├─────────────────────────────────────┤
│ Monitoring Settings                 │
│ ┌─────────────────────────────────┐ │
│ │ Check Interval: [5] seconds    │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Notification Settings               │
│ ┌─────────────────────────────────┐ │
│ │ ☑ Enable Toast Notifications   │ │
│ │ ☐ Enable Sound Alerts          │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Logging                             │
│ ┌─────────────────────────────────┐ │
│ │ ☑ Enable Logging               │ │
│ │ Log Level: [Info ▼]            │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Migration                           │
│ ┌─────────────────────────────────┐ │
│ │ ☑ Auto-migrate old settings    │ │
│ │ [Migrate Now]                   │ │
│ └─────────────────────────────────┘ │
│                                     │
│ [Save] [Cancel]                     │
└─────────────────────────────────────┘
```

### 진단 창
```
┌─────────────────────────────────────┐
│ SyncGuard Diagnostics               │
├─────────────────────────────────────┤
│ System Information                  │
│ ┌─────────────────────────────────┐ │
│ │ OS: Windows 11 22H2            │ │
│ │ GPU: RTX 5080                  │ │
│ │ Driver: 546.33                 │ │
│ └─────────────────────────────────┘ │
│                                     │
│ Monitoring Methods                  │
│ ┌─────────────────────────────────┐ │
│ │ ✅ NVAPI                        │ │
│ │ ✅ Performance Counters         │ │
│ │ ❌ nvidia-smi                   │ │
│ └─────────────────────────────────┘ │
│                                     │
│ [Generate Report] [Copy to Clipboard] │
└─────────────────────────────────────┘
```

---

## 🧪 테스트 계획

### 로컬 테스트 (RTX 5080)
- ✅ 기본 UI 동작 확인
- ✅ NVAPI 호출 테스트
- ✅ 설정 저장/로드 테스트
- ✅ 로깅 시스템 테스트
- 🔄 설정 마이그레이션 테스트
- 🔄 진단 도구 테스트
- 🔄 권한 관리 테스트

### 실제 환경 테스트 (별도 서버)
- 🔄 Quadro Sync 하드웨어 테스트
- 🔄 멀티 GPU 환경 테스트
- 🔄 실제 동기화 상태 확인
- 🔄 성능 테스트
- 🔄 원격 모니터링 테스트

---

## 📝 다음 단계

1. **프로젝트 구조 생성**
   ```bash
   dotnet new sln -n SyncGuard
   dotnet new classlib -n SyncGuard.Core
   dotnet new winforms -n SyncGuard.Tray
   dotnet new classlib -n SyncGuard.Settings
   dotnet new classlib -n SyncGuard.Common
   dotnet new classlib -n SyncGuard.Remote
   ```

2. **기본 트레이 아이콘 구현**
3. **Sync 상태 확인 로직 구현**
4. **설정 기능 추가**
5. **마이그레이션 시스템 구현**
6. **진단 도구 개발**
7. **권한 관리 시스템 구현**

---

## 📚 참고 자료

### 소스 코드 분석
- **nDisplay**: `SampleCode/nDisplay/Source/DisplayClusterStageMonitoring/Private/NvidiaSyncWatchdog.cpp`
- **Switchboard**: `SampleCode/Switchboard/Source/Switchboard/switchboard/devices/ndisplay/ndisplay_monitor.py`

### 문서
- **Quadro Sync 모니터링 가이드**: `Docs/quadro-sync-monitoring-complete-guide.md`

---

## 🔄 업데이트 기록

- **2024-01-XX**: 초기 개발 계획 작성
- **2024-01-XX**: nDisplay/Switchboard 소스 코드 분석 완료
- **2024-01-XX**: 기술 스택 및 아키텍처 결정
- **2024-01-XX**: 추가 고려사항 (마이그레이션, 원격 모니터링, 진단 도구, 권한 관리) 추가

---

*이 문서는 SyncGuard_V3 프로젝트의 개발 가이드라인입니다. 프로젝트 진행에 따라 지속적으로 업데이트됩니다.* 