# SyncGuard_V3 프로젝트 전체 요약

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

---

## 🔍 핵심 발견사항 (nDisplay/Switchboard 분석)

### **WDDM에서 작동하는 NVAPI 함수들 발견**

#### **nDisplay 분석 결과**
```cpp
// 실제로 WDDM에서 작동하는 함수들
NvAPI_D3D1x_QueryFrameCount()           // 프레임 카운터 쿼리
NvAPI_D3D12_QueryPresentBarrierSupport() // Present Barrier 지원 확인
NvAPI_QueryPresentBarrierFrameStatistics() // Present Barrier 통계
```

#### **핵심 파일들**
- `NvidiaSyncWatchdog.cpp` - 프레임 카운터 기반 동기화 품질 측정
- `DisplayClusterRenderSyncPolicyNvidiaPresentBarrierWindows.cpp` - Present Barrier 구현
- `DisplayClusterRenderSyncPolicyNvidiaSwapBarrierWindows.cpp` - Swap Barrier 구현

### **Switchboard 분석 결과**

#### **Sync 토폴로지 정보 수집**
```python
# 실제 수집하는 데이터 구조
gpu_sync_oks = [gpu['bIsSynced'] for gpu in sync_topo['syncGpus']]
house_syncs = [syncTopo['syncStatusParams']['bHouseSync'] for syncTopo in sync_topos]
sync_sources = [sync_topo['syncControlParams']['source'] for sync_topo in sync_topos]
```

#### **핵심 파일들**
- `ndisplay_monitor.py` - Sync 상태 모니터링 로직
- `message_protocol.py` - SyncStatusRequestFlags 정의
- `plugin_ndisplay.py` - nDisplay 플러그인 설정

---

## 🏗️ 기술 결정사항

### **개발 언어: C#/.NET**
**선택 이유:**
- NVAPI 바인딩 가능 (NvAPIWrapper 라이브러리)
- 트레이 아이콘 구현 용이 (System.Windows.Forms.NotifyIcon)
- Windows 네이티브 기능 활용 (Performance Counters, WMI)
- 개발 속도와 성능의 균형

### **구현 우선순위**
1. **nDisplay 방식** (우선순위 1) - NVAPI 직접 호출
2. **Switchboard 방식** (우선순위 2) - Sync 토폴로지 정보
3. **문서 방식** (우선순위 3) - Performance Counters, nvidia-smi

---

## 📁 프로젝트 구조

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

## 💻 핵심 구현 방법

### **1. nDisplay 방식 (가장 중요)**

#### **프레임 카운터 기반 동기화 품질 측정**
```csharp
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

#### **Present Barrier 통계 수집**
```csharp
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

### **2. Switchboard 방식**

#### **Sync 토폴로지 정보 수집**
```csharp
public class SyncTopologyMonitor
{
    public SyncTopologyInfo GetSyncTopology()
    {
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

### **3. 대안 방법들**

#### **Windows Performance Counters**
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

#### **nvidia-smi XML 파싱**
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

## 📊 데이터 모델

### **SyncStatus 클래스**
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

### **SyncTopologyInfo 클래스**
```csharp
public class SyncTopologyInfo
{
    public bool[][] GpuSyncStatus { get; set; } // 각 Sync 그룹별 GPU 동기화 상태
    public bool[] HouseSyncStatus { get; set; } // House Sync 연결 상태
    public string[] SyncSource { get; set; }    // Sync 소스 (Vsync/House)
    public float[] RefreshRates { get; set; }   // 각 Sync 그룹별 리프레시 레이트
}
```

---

## 🎨 UI/UX 설계

### **트레이 아이콘 상태**
- **🟢 녹색**: 동기화 정상 (IsHardwareSyncEnabled && IsFrameLockActive)
- **🟡 노란색**: 하드웨어 준비됨 (IsHardwareSyncEnabled && !IsFrameLockActive)
- **🔴 빨간색**: 동기화 비활성 (!IsHardwareSyncEnabled)

### **컨텍스트 메뉴**
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

### **설정 창 주요 항목**
- **모니터링 간격**: 1-60초 설정
- **알림 설정**: 토스트 알림, 사운드 알림
- **로깅 설정**: 로그 레벨, 파일 위치
- **마이그레이션**: 이전 버전 설정 자동 가져오기

---

## 🛠️ 기술 스택

### **필수 NuGet 패키지**
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

### **개발 도구**
- **IDE**: Visual Studio 2022
- **.NET**: .NET 8.0
- **빌드**: MSBuild
- **버전 관리**: Git

---

## 🎯 개발 단계

### **Phase 1: 기본 기능 (1-2주)**
1. ✅ 프로젝트 구조 생성
2. ✅ 트레이 아이콘 표시
3. ✅ Sync 상태 확인 (기본)
4. ✅ 컨텍스트 메뉴
5. ✅ 설정 창

### **Phase 2: 고급 기능 (2-3주)**
1. 🔄 NVAPI 직접 호출 구현
2. 🔄 대안 방법들 구현
3. 🔄 로깅 시스템
4. 🔄 알림 시스템
5. 🔄 설정 저장/로드

### **Phase 3: 확장 기능 (2-3주)**
1. 🔄 설정 마이그레이션 구현
2. 🔄 진단 도구 개발
3. 🔄 권한 관리 시스템
4. 🔄 원격 모니터링 기반 구조

### **Phase 4: 최적화 및 테스트 (1-2주)**
1. 🔄 성능 최적화
2. 🔄 에러 처리 강화
3. 🔄 실제 환경 테스트
4. 🔄 배포 준비

---

## 🔧 추가 고려사항 상세

### **1. 설정 마이그레이션**
- **V1 설정**: `%APPDATA%\SyncGuard\config.ini`
- **V2 설정**: `%APPDATA%\SyncGuard\settings.json`
- **자동 감지 및 변환**
- **마이그레이션 실패 시 에러 처리**

### **2. 원격 모니터링 준비**
- **SignalR 기반 통신**
- **중앙 서버 구조 설계**
- **다중 서버 상태 집계**
- **실시간 알림 시스템**

### **3. 진단 도구**
- **시스템 정보 수집**
- **GPU 정보 수집**
- **모니터링 방법 가용성 체크**
- **최근 로그 포함**
- **지원팀용 리포트 생성**

### **4. 최소 권한 실행**
- **일반 권한**: 기본 모니터링, NVAPI 접근
- **관리자 권한**: 레지스트리 수정, 서비스 설치
- **권한별 기능 분리**
- **권한 부족 시 안내 메시지**

---

## 🧪 테스트 계획

### **로컬 테스트 (RTX 5080)**
- ✅ 기본 UI 동작 확인
- ✅ NVAPI 호출 테스트
- ✅ 설정 저장/로드 테스트
- ✅ 로깅 시스템 테스트
- 🔄 설정 마이그레이션 테스트
- 🔄 진단 도구 테스트
- 🔄 권한 관리 테스트

### **실제 환경 테스트 (별도 서버)**
- 🔄 Quadro Sync 하드웨어 테스트
- 🔄 멀티 GPU 환경 테스트
- 🔄 실제 동기화 상태 확인
- 🔄 성능 테스트
- 🔄 원격 모니터링 테스트

---

## 📝 시작 명령어

### **프로젝트 생성**
```bash
# 솔루션 생성
dotnet new sln -n SyncGuard

# 프로젝트들 생성
dotnet new classlib -n SyncGuard.Core
dotnet new winforms -n SyncGuard.Tray
dotnet new classlib -n SyncGuard.Settings
dotnet new classlib -n SyncGuard.Common
dotnet new classlib -n SyncGuard.Remote

# 프로젝트 추가
dotnet sln add SyncGuard.Core/SyncGuard.Core.csproj
dotnet sln add SyncGuard.Tray/SyncGuard.Tray.csproj
dotnet sln add SyncGuard.Settings/SyncGuard.Settings.csproj
dotnet sln add SyncGuard.Common/SyncGuard.Common.csproj
dotnet sln add SyncGuard.Remote/SyncGuard.Remote.csproj
```

### **필수 패키지 설치**
```bash
# SyncGuard.Core
dotnet add SyncGuard.Core/SyncGuard.Core.csproj package NvAPIWrapper
dotnet add SyncGuard.Core/SyncGuard.Core.csproj package System.Management

# SyncGuard.Tray
dotnet add SyncGuard.Tray/SyncGuard.Tray.csproj package Microsoft.WindowsAppSDK
dotnet add SyncGuard.Tray/SyncGuard.Tray.csproj package Microsoft.Windows.SDK.BuildTools

# SyncGuard.Common
dotnet add SyncGuard.Common/SyncGuard.Common.csproj package Serilog
dotnet add SyncGuard.Common/SyncGuard.Common.csproj package Serilog.Sinks.File

# SyncGuard.Remote
dotnet add SyncGuard.Remote/SyncGuard.Remote.csproj package SignalR.Client
dotnet add SyncGuard.Remote/SyncGuard.Remote.csproj package Newtonsoft.Json
```

---

## 📚 참고 자료

### **소스 코드 분석**
- **nDisplay**: `SampleCode/nDisplay/Source/DisplayClusterStageMonitoring/Private/NvidiaSyncWatchdog.cpp`
- **Switchboard**: `SampleCode/Switchboard/Source/Switchboard/switchboard/devices/ndisplay/ndisplay_monitor.py`

### **문서**
- **Quadro Sync 모니터링 가이드**: `Docs/quadro-sync-monitoring-complete-guide.md`
- **개발 계획**: `Docs/syncguard-development-plan.md`

### **핵심 NVAPI 함수들**
```cpp
// WDDM에서 작동하는 함수들
NvAPI_D3D1x_QueryFrameCount()           // 프레임 카운터 쿼리
NvAPI_D3D12_QueryPresentBarrierSupport() // Present Barrier 지원 확인
NvAPI_QueryPresentBarrierFrameStatistics() // Present Barrier 통계
NvAPI_D3D1x_QueryMaxSwapGroup()         // Swap Group 최대값 쿼리
NvAPI_D3D1x_JoinSwapGroup()             // Swap Group 참가
NvAPI_D3D1x_BindSwapBarrier()           // Swap Barrier 바인딩
```

---

## ⚠️ 주의사항

### **WDDM 환경 제약사항**
- **일부 NVAPI 함수는 TCC 모드에서만 작동**
- **WDDM에서는 직접적인 하드웨어 제어 불가**
- **Performance Counters나 간접적 방법 필요**

### **권한 요구사항**
- **일반 권한**: 기본 모니터링 기능
- **관리자 권한**: 고급 설정, 레지스트리 수정
- **서비스 설치**: 관리자 권한 필요

### **성능 고려사항**
- **NVAPI 호출은 성능에 영향 가능**
- **적절한 폴링 간격 설정 필요**
- **리소스 사용량 모니터링**

---

## 🔄 업데이트 기록

- **2024-01-XX**: 초기 프로젝트 기획
- **2024-01-XX**: nDisplay/Switchboard 소스 코드 분석
- **2024-01-XX**: WDDM에서 작동하는 NVAPI 함수 발견
- **2024-01-XX**: 기술 스택 및 아키텍처 결정
- **2024-01-XX**: 추가 고려사항 (마이그레이션, 원격 모니터링, 진단 도구, 권한 관리) 추가
- **2024-01-XX**: 전체 프로젝트 요약 문서 작성

---

## 📞 다음 단계

1. **실제 환경으로 이동**
2. **프로젝트 구조 생성**
3. **기본 트레이 아이콘 구현**
4. **NVAPI 연동 테스트**
5. **단계별 기능 구현**

---

*이 문서는 SyncGuard_V3 프로젝트의 전체 논의 내용을 요약한 것입니다. 실제 개발 시 참고 자료로 활용하세요.* 