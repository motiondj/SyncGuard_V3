# NVIDIA Quadro Sync 모니터링 개발 완전 가이드

## 목차
1. [개요](#개요)
2. [문제 상황](#문제-상황)
3. [모니터링 솔루션 아키텍처](#모니터링-솔루션-아키텍처)
4. [WDDM 환경에서의 제약사항](#wddm-환경에서의-제약사항)
5. [실제 작동하는 모니터링 방법들](#실제-작동하는-모니터링-방법들)
6. [레지스트리 접근 시 주의사항](#레지스트리-접근-시-주의사항)
7. [언리얼엔진 소스 코드 분석](#언리얼엔진-소스-코드-분석)
8. [구현 권장사항](#구현-권장사항)

---

## 개요

### 프로젝트 목표
NVIDIA RTX 6000 Ada Generation과 Quadro Sync 카드를 사용하는 멀티 서버 환경에서 동기화 상태를 모니터링하는 독립적인 소프트웨어 개발

### 핵심 요구사항
- **동기화 연결 상태 확인**: 데이지체인으로 연결된 Sync 카드들의 연결 상태
- **Master/Slave 관계 확인**: 어느 GPU가 Master이고 어느 것이 Slave인지
- **Sync Lock 상태**: 실제로 동기화가 이루어지고 있는지 (Synced 상태)
- **트레이 아이콘 표시**: 간단한 상태 표시
- **토스트 알림**: 문제 발생 시에만 알림

### 시스템 환경
- GPU: NVIDIA RTX 6000 Ada Generation
- 동기화: Quadro Sync 카드 (데이지체인 연결)
- OS: Windows (WDDM 모드)
- 관련 SW: 언리얼엔진 nDisplay (Switchboard 미사용)

---

## 문제 상황

### NVAPI의 한계
WDDM(Windows Display Driver Model) 모드에서는 NVAPI의 Quadro Sync 관련 함수들이 작동하지 않습니다:
- `NvAPI_GSync_EnumSyncDevices()` - 실패
- `NvAPI_GSync_GetStatusParameters()` - 실패
- 대부분의 Sync 관련 API는 TCC 모드에서만 작동

### TCC 모드 전환 불가
- TCC 모드로 전환 시 디스플레이 출력이 완전히 비활성화됨
- nDisplay를 사용한 멀티 서버 렌더링에는 디스플레이 출력이 필수
- 따라서 WDDM 모드에서 작동하는 대안이 필요

---

## 모니터링 솔루션 아키텍처

### 추천 아키텍처: 3계층 하이브리드 구조

#### 1. 데이터 수집 계층 (각 서버)
- 경량 에이전트를 각 GPU 서버에 배치
- 로컬 Sync 상태를 다양한 방법으로 수집
- 기본적인 필터링과 버퍼링 수행

#### 2. 집계 계층 (옵션)
- 여러 서버의 데이터를 중앙에서 수집
- 서버 간 동기화 상태 비교
- 전체 시스템의 동기화 품질 평가

#### 3. 프레젠테이션 계층
- Windows 트레이 아이콘 애플리케이션
- 토스트 알림 시스템
- 간단한 상태 표시 UI

### 기술 스택 권장사항
- **개발 언어**: C#/.NET with WinUI 3
- **통신**: gRPC (멀티 서버 환경인 경우)
- **모니터링**: Performance Counters, WMI, nvidia-smi
- **알림**: Windows App SDK의 AppNotificationManager

---

## WDDM 환경에서의 제약사항

### 작동하지 않는 것들
1. **NVAPI Sync 함수들**
   - 대부분의 GSync/FrameLock 관련 API
   - 하드웨어 직접 접근 함수들

2. **커널 레벨 접근**
   - WDDM에서는 디스플레이 드라이버를 Windows가 관리
   - 직접적인 하드웨어 제어 불가

### 대안적 접근 필요
- 높은 수준의 API 사용 (Performance Counters, WMI)
- 간접적인 상태 확인 (nvidia-smi, 레지스트리)
- 소프트웨어 기반 동기화 품질 측정

---

## 실제 작동하는 모니터링 방법들

### 1. NVIDIA-SMI를 통한 확인

#### 기본 명령어
```bash
# Sync 관련 정보 검색
nvidia-smi -q | findstr /i "sync"
nvidia-smi -q | findstr /i "frame lock"
nvidia-smi -q | findstr /i "timing"

# XML 출력으로 더 자세한 정보 확인
nvidia-smi -q -x > sync_info.xml
```

#### 특징
- 일부 드라이버 버전에서만 Sync 정보 제공
- 엔터프라이즈 드라이버에서 더 많은 정보 제공
- XML 출력에 숨겨진 정보가 있을 수 있음

### 2. Windows Performance Counters (가장 유망)

#### 확인 방법
```powershell
# Performance Counter 목록 확인
Get-Counter -ListSet "*" | Where-Object {$_.CounterSetName -like "*NVIDIA*"}

# Sync 관련 카운터 검색
Get-Counter -ListSet "*" | ForEach-Object {
    $_.Counter | Where-Object {$_ -like "*sync*" -or $_ -like "*frame*lock*"} 
}
```

#### 주요 카운터들
- `\NVIDIA GPU(*)\Frame Lock Status` - 0(unlocked) 또는 1(locked)
- `\NVIDIA GPU(*)\Sync Status`
- `\NVIDIA GPU(*)\Timing Reference`
- `\NVIDIA GPU(*)\Master Slave Status`

#### C# 구현 예제
```csharp
using System.Diagnostics;

PerformanceCounter syncCounter = new PerformanceCounter(
    "NVIDIA GPU",           // 카테고리
    "Frame Lock Status",    // 카운터 이름
    "GPU 0"                 // 인스턴스
);

float syncStatus = syncCounter.NextValue();
// 0 = Unlocked, 1 = Locked
```

### 3. NVWMI (NVIDIA WMI Classes)

#### 네임스페이스 탐색
```powershell
# NVWMI 클래스 확인
Get-WmiObject -Namespace "root\CIMV2\NV" -List | 
    Where-Object {$_.Name -like "*Sync*"}

# 가능한 네임스페이스들
$namespaces = @("root\CIMV2\NV", "root\NVIDIA", "root\WMI")
```

#### 특징
- 드라이버 버전에 따라 가용성이 다름
- 완전한 정보를 제공하지 않을 수 있음

### 4. 간접적인 동기화 품질 측정

#### 타이밍 기반 측정
```csharp
// 각 서버에서 Present 타이밍 수집
// 서버 간 타이밍 차이로 동기화 품질 평가
public class SyncQualityMonitor
{
    // Frame 간격 변동성 측정
    // 서버 간 위상차 계산
    // 누적 드리프트 추적
}
```

### 5. 기타 확인 방법들

#### 레지스트리 읽기
```powershell
# Quadro Sync 설정 확인
Get-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Video\*\0000" | 
    Where-Object {$_ -match "sync"}
```

#### PnP 디바이스 확인
```powershell
# Quadro Sync 카드 인식 여부
Get-PnpDevice | Where-Object {$_.FriendlyName -like "*Quadro Sync*"}
```

---

## 레지스트리 접근 시 주의사항

### 안전한 작업
- **읽기 작업은 항상 안전**
- 모니터링 목적의 읽기 전용 접근
- 충분한 폴링 간격 (1초 이상)

### 위험한 작업
- **컨텐츠 실행 중 Sync 관련 레지스트리 수정**
  - Sync Lock이 깨질 수 있음
  - 예측 불가능한 동작 발생 가능
  - 크래시 위험

### 재부팅 필요 여부
- **즉시 적용**: 애플리케이션 레벨 설정
- **재부팅 필요**: 드라이버 레벨 설정, Quadro Sync 토폴로지

### 권장사항
```csharp
// 읽기 전용으로만 접근
using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path, false))
{
    // 안전한 읽기 작업
}
```

---

## 언리얼엔진 소스 코드 분석

### nDisplay 플러그인 위치
```
Engine/Plugins/Runtime/nDisplay/
├── Source/
│   ├── DisplayCluster/
│   │   └── Private/Render/Synchronization/
│   └── DisplayClusterConfigurator/
```

### Switchboard 위치
```
Engine/Plugins/VirtualProduction/Switchboard/
├── Source/
│   └── Switchboard/ (Python 기반)
```

### 핵심 파일들
- `DisplayClusterRenderSyncPolicyNvidia.cpp`
- `DisplayClusterRenderSyncPolicyBase.cpp`
- `switchboard_ndisplay.py`
- `device_manager.py`

### 검색해야 할 패턴
```cpp
// C++ 코드에서
NvAPI_
GetSyncStatus()
CheckFrameLock()
PerformanceCounter

// Python 코드에서
import win32pdh  # Performance counter
import wmi       # WMI 접근
```

### 소스 코드 검색 방법
```powershell
# 로컬 엔진 소스에서 검색
$enginePath = "C:\Program Files\Epic Games\UE_5.0\Engine"

Get-ChildItem -Path "$enginePath\Plugins" -Recurse -Include *.cpp,*.h,*.py | 
    Select-String -Pattern "sync|frame.*lock|nvapi|performance.*counter"
```

---

## 구현 권장사항

### 1단계: 하드웨어 확인
1. Quadro Sync 카드 인식 여부 확인
2. 연결된 GPU 목록 확인
3. 기본적인 토폴로지 파악

### 2단계: 모니터링 방법 선택
1. **우선순위 1**: Windows Performance Counters
2. **우선순위 2**: nvidia-smi XML 파싱
3. **보조 수단**: 타이밍 기반 간접 측정

### 3단계: 트레이 애플리케이션 개발
```csharp
// 기본 구조
public class QuadroSyncMonitor : ApplicationContext
{
    private NotifyIcon trayIcon;
    private Timer monitoringTimer;
    
    // Performance Counter 또는 다른 방법으로 상태 확인
    private void CheckSyncStatus()
    {
        // Sync 상태 확인 로직
        // 문제 발생 시 토스트 알림
    }
}
```

### 4단계: 확장성 고려
- 플러그인 아키텍처로 다양한 모니터링 방법 지원
- 새로운 서버 추가 시 자동 발견
- 설정 파일 기반 구성

### 성능 최적화
- 이벤트 기반 모니터링 우선
- 적응적 폴링 간격
- 리소스 사용량 최소화 (목표: <5% CPU, <30MB 메모리)

---

## 결론

WDDM 환경에서 NVAPI가 작동하지 않는 제약에도 불구하고, Windows Performance Counters, nvidia-smi, WMI 등 다양한 대안을 통해 Quadro Sync 상태를 모니터링할 수 있습니다. 

가장 현실적인 접근법은:
1. Performance Counters로 Frame Lock Status 확인
2. nvidia-smi로 보조 정보 수집
3. 필요시 타이밍 기반 간접 측정

언리얼엔진의 nDisplay 소스 코드를 분석하면 더 정확한 구현 방법을 찾을 수 있을 것입니다.