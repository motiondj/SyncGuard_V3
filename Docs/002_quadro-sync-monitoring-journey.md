# NVIDIA Quadro Sync 모니터링 개발 여정과 교훈

## 📋 프로젝트 개요

### 초기 목표
NVIDIA RTX 6000 Ada Generation과 Quadro Sync 카드를 사용하는 멀티 서버 환경에서 동기화 상태를 모니터링하는 트레이 애플리케이션 개발

### 핵심 요구사항
- 데이지체인으로 연결된 Sync 카드들의 연결 상태 확인
- Master/Slave 관계 파악
- Sync Lock 상태 실시간 모니터링
- 문제 발생 시 토스트 알림

---

## 🚧 직면한 문제들

### 1. NVAPI 접근 불가
```csharp
// ❌ WDDM 모드에서 작동하지 않음
NvAPI_Initialize(); // 실패
NvAPI_GSync_EnumSyncDevices(); // 실패
NvAPI_D3D1x_QueryFrameCount(); // 실패
```

**원인**:
- WDDM(Windows Display Driver Model)에서는 대부분의 Quadro Sync API 제한
- TCC 모드 필요하지만, 디스플레이 출력이 필요한 환경에서는 사용 불가
- D3D 컨텍스트가 있어도 접근 불가

### 2. NVAPI 함수를 찾을 수 없는 문제
```csharp
// ❌ 일반적인 DLL 방식으로는 작동 안 함
[DllImport("nvapi64.dll")]
public static extern int NvAPI_Initialize(); // 함수를 찾을 수 없음
```

**원인**:
- NVAPI는 QueryInterface 패턴 사용
- 모든 함수가 ID로 관리됨
- 직접 export되지 않음

---

## 🔍 시도한 해결 방법들

### 1. Performance Counters
```powershell
# 가능성 있는 카운터들
"\NVIDIA GPU(*)\Frame Lock Status"
"\NVIDIA GPU(*)\Sync Status"
```
**결과**: 시스템에 따라 있을 수도, 없을 수도 있음

### 2. nvidia-smi
```bash
nvidia-smi -q | findstr /i "sync"
nvidia-smi -q -x  # XML 출력
```
**결과**: 일부 드라이버/하드웨어에서만 sync 정보 제공

### 3. WMI (Windows Management Instrumentation)
```powershell
Get-WmiObject -Namespace "root\CIMV2\NV" -List
```
**결과**: 매우 제한적인 정보만 제공

### 4. 레지스트리
```powershell
HKLM:\SYSTEM\CurrentControlSet\Control\Video\*
```
**결과**: 설정값은 있지만 실시간 상태는 없음

---

## 💡 아키텍처 진화

### 1단계: 중앙집중식 (초기 시도)
```
중앙 서버 → 모든 GPU 서버에 직접 접근 → Sync 상태 확인
```
**문제**: NVAPI 접근 불가로 실패

### 2단계: 분산형 (개선된 접근)
```
각 서버에서 자체 상태 확인 → 중앙으로 보고 → 종합 분석
```
**문제**: 각 서버도 자신의 sync 상태를 확인하기 어려움

### 3단계: 현실적 타협
```
각 서버에서 가능한 정보만 수집 → 간접적으로 sync 상태 추정
```

---

## 🎓 핵심 깨달음

### 1. 언리얼엔진 nDisplay와 Switchboard의 작동 방식
- **Switchboard**: Python 기반 중앙 제어
- **Switchboard Listener**: 각 노드에서 실행되는 에이전트
- 각자가 로컬 상태를 파악하고 중앙에 보고하는 구조

### 2. 왜 이렇게 어려운가?
- NVIDIA가 의도적으로 전문가용 기능 제한
- Windows WDDM의 보안 모델
- 문서화 부족

### 3. 잘못된 가정들
- ❌ "중앙에서 모든 서버의 sync 상태를 직접 확인할 수 있을 것이다"
- ❌ "NVAPI를 사용하면 모든 정보를 얻을 수 있을 것이다"
- ❌ "전체 데이지체인 토폴로지를 한 번에 파악할 수 있을 것이다"

---

## 🛠️ 현실적인 구현 방안

### Plan A: 최소 기능 구현
```csharp
public class MinimalSyncMonitor
{
    // 1. 기본 연결 상태만 확인
    public bool CheckConnectivity() => Ping(upstreamServer);
    
    // 2. 프로세스 실행 여부
    public bool IsNDisplayRunning() => Process.GetProcessesByName("nDisplay").Any();
    
    // 3. 설정 파일 검증
    public bool ValidateConfig() => File.Exists(configPath) && IsValidJson(configPath);
}
```

### Plan B: 간접 모니터링
```csharp
public class IndirectSyncMonitor
{
    // 1. 로그 파일 모니터링
    public void MonitorLogs()
    {
        // nDisplay 로그에서 sync 관련 에러 검색
        // Frame drop 패턴 분석
    }
    
    // 2. 성능 지표 모니터링
    public void MonitorPerformance()
    {
        // 비정상적인 GPU 사용 패턴
        // 프레임 타이밍 일관성
    }
}
```

### Plan C: 하이브리드 접근
```csharp
public class HybridSyncMonitor
{
    private List<ISyncMethod> methods = new()
    {
        new PerformanceCounterMethod(),
        new NvidiaSmiMethod(),
        new LogFileMethod(),
        new ProcessMonitorMethod()
    };
    
    public SyncStatus GetStatus()
    {
        foreach (var method in methods)
        {
            if (method.IsAvailable())
                return method.GetStatus();
        }
        
        return new SyncStatus { Status = "Unknown" };
    }
}
```

---

## 🎯 권장사항

### 1. 단계적 접근
1. **Phase 1**: 작동 확인 가능한 기본 기능
   - 서버 연결 상태
   - 프로세스 실행 여부
   - 설정 파일 유효성

2. **Phase 2**: 간접적 sync 모니터링
   - 로그 분석
   - 성능 패턴
   - 알려진 문제 증상

3. **Phase 3**: 고급 기능 (가능한 경우)
   - 실제 sync 상태
   - 토폴로지 재구성
   - 예측적 문제 감지

### 2. 대안 탐색
- GPU-Z, HWiNFO 같은 도구 분석
- ETW (Event Tracing for Windows) 활용
- NVIDIA Enterprise Support 문의
- 커뮤니티 솔루션 조사

### 3. 현실적 기대치
- 완벽한 sync 모니터링은 불가능할 수 있음
- 부분적 솔루션도 가치가 있음
- 간접적 모니터링도 실용적일 수 있음

---

## 📚 참고 자료

### 기술 문서
- NVIDIA NVAPI Documentation (제한적)
- Unreal Engine nDisplay Source Code
- Switchboard Python Source Code

### 커뮤니티
- NVIDIA Developer Forums
- Unreal Engine Forums - nDisplay Section
- Stack Overflow - NVAPI Tags

---

## 🔄 향후 방향

### 즉시 구현 가능
- 기본 연결 상태 모니터링
- 프로세스 모니터링
- 로그 파일 분석

### 추가 연구 필요
- ETW를 통한 드라이버 이벤트 캡처
- 언리얼엔진 플러그인으로 구현
- 대체 하드웨어 API 조사

### 장기 목표
- NVIDIA와 협력하여 공식 API 접근 권한 획득
- 오픈소스 커뮤니티 솔루션 개발
- 대체 동기화 검증 방법 확립

---

## 💭 결론

NVIDIA Quadro Sync 모니터링은 예상보다 훨씬 어려운 과제입니다. WDDM 환경에서의 제약, 문서화 부족, API 접근 제한 등 여러 장벽이 있습니다.

하지만 완벽한 솔루션이 없더라도, 부분적이고 간접적인 모니터링으로도 실용적인 가치를 제공할 수 있습니다. 중요한 것은 현실적인 목표를 설정하고, 가능한 것부터 차근차근 구현해 나가는 것입니다.

이 여정에서 얻은 가장 큰 교훈은: **때로는 문제를 다른 각도에서 바라보고, 완벽하지 않더라도 실용적인 해결책을 찾는 것이 더 중요하다**는 것입니다.