# SyncGuard V3 개발 분석 문서

## 1. 프로젝트 개요

### 1.1 목적
- NVIDIA Quadro Sync II 카드를 사용하는 멀티 서버 환경에서 디스플레이 동기화 상태를 모니터링하는 트레이 앱
- NVIDIA 제어판의 디스플레이 동기화 설정(마스터/슬레이브, 디스플레이 선택 등)이 정상인지 코드로 진단

### 1.2 기술 스택
- **언어**: C# (.NET 9.0)
- **UI**: Windows Forms (트레이 앱)
- **API**: 
  - WMI (Windows Management Instrumentation)
  - NVAPI (NVIDIA API)
- **아키텍처**: 
  - SyncGuard.Core: 핵심 로직
  - SyncGuard.Tray: 트레이 UI

## 2. 데이터 소스 분석

### 2.1 WMI (Windows Management Instrumentation)

#### 2.1.1 SyncTopology 클래스
**네임스페이스**: `root\CIMV2\NV`

**사용 가능한 속성들**:
```
count: 0
displaySyncState: 1
id: 1001
isDisplayMasterable: True
name: (빈 문자열)
ordinal: 1
uname: invalid
ver: System.Management.ManagementBaseObject
```

#### 2.1.2 각 속성 상세 분석

**displaySyncState**
- **값**: 0 또는 1
- **의미**: 
  - 0: 디스플레이 동기화 꺼짐
  - 1: 디스플레이 동기화 켜짐 (디스플레이 선택됨)
- **신뢰도**: 높음 (일관된 값 반환)
- **사용 여부**: 현재 로직의 핵심 속성

**isDisplayMasterable**
- **값**: True 또는 False
- **의미**: 이 시스템이 Master가 될 수 있는 능력
- **신뢰도**: 낮음 (항상 True로 나옴)
- **문제점**: 실제 Master/Slave 상태를 반영하지 않음
- **사용 여부**: 현재 사용 중이지만 재검토 필요

**count**
- **값**: 0
- **의미**: 디스플레이 리스트 개수
- **신뢰도**: 낮음 (항상 0으로 나옴)
- **사용 여부**: 현재 사용하지 않음

**name**
- **값**: 빈 문자열
- **의미**: 시스템 이름
- **신뢰도**: 낮음 (항상 빈 문자열)
- **사용 여부**: 현재 사용하지 않음

**uname**
- **값**: "invalid"
- **의미**: 사용자 이름
- **신뢰도**: 낮음 (항상 "invalid")
- **사용 여부**: 현재 사용하지 않음

**ver**
- **값**: ManagementBaseObject
- **의미**: 버전 정보
- **신뢰도**: 미확인 (내부 속성 분석 필요)
- **사용 여부**: 현재 사용하지 않음

**id**
- **값**: 1001
- **의미**: 시스템 ID
- **신뢰도**: 높음 (일관된 값 반환)
- **사용 여부**: 현재 사용하지 않음

**ordinal**
- **값**: 1
- **의미**: 순서
- **신뢰도**: 높음 (일관된 값 반환)
- **사용 여부**: 현재 사용하지 않음

### 2.2 NVAPI (NVIDIA API)

#### 2.2.1 사용 가능한 정보
- GPU 정보
- 드라이버 정보
- Quadro Sync II 카드 상태
- 동기화 관련 하드웨어 정보

#### 2.2.2 제한사항
- Master/Slave 설정 정보 직접 접근 불가
- 디스플레이 선택 상태 직접 확인 불가
- 보조 정보로만 활용 가능

## 3. 현재 동기화 상태 판단 로직

### 3.1 현재 조건 (2025-06-25 기준)

```csharp
// 1. displaySyncState 확인
if (displaySyncState == 0) {
    // 동기화 안됨 (파란색)
    return SyncStatus.Free;
}

// 2. isDisplayMasterable로 Master/Slave 판단
if (isDisplayMasterable == true) {
    // Master 케이스
    if (userRole == SyncRole.Master) {
        // 설정: Master, 실제: Master → 동기화됨 (초록색)
        return SyncStatus.Synced;
    } else {
        // 설정: Slave, 실제: Master → 설정 충돌 (빨간색)
        return SyncStatus.ConfigConflict;
    }
} else {
    // Slave 케이스
    if (userRole == SyncRole.Slave) {
        // 설정: Slave, 실제: Slave → 동기화됨 (초록색)
        return SyncStatus.Synced;
    } else {
        // 설정: Master, 실제: Slave → 설정 충돌 (빨간색)
        return SyncStatus.ConfigConflict;
    }
}
```

### 3.2 문제점 분석

#### 3.2.1 isDisplayMasterable 속성 문제
- **현상**: 항상 `True`로 반환
- **원인**: 실제 Master/Slave 상태가 아닌 "Master가 될 수 있는 능력"을 나타냄
- **영향**: Master/Slave 판단이 부정확함

#### 3.2.2 다른 속성들의 한계
- `count`: 항상 0 (디스플레이 리스트 정보 부족)
- `name`: 항상 빈 문자열 (시스템 식별 불가)
- `uname`: 항상 "invalid" (사용자 정보 부족)

## 4. 대안 방안 검토

### 4.1 다른 WMI 클래스 탐색
- **SyncTopology 외 다른 클래스**: 아직 확인하지 않음
- **NVIDIA 관련 다른 WMI 클래스**: 확인 필요
- **시스템 정보 관련 클래스**: 확인 필요

### 4.2 레지스트리 기반 접근
- **NVIDIA 제어판 설정**: 레지스트리에 저장될 가능성
- **경로**: `HKEY_LOCAL_MACHINE\SOFTWARE\NVIDIA Corporation\` 등
- **장점**: 직접적인 설정 값 접근 가능
- **단점**: 레지스트리 구조 파악 필요

### 4.3 NVAPI 확장 활용
- **현재**: 기본 정보만 활용
- **확장**: 더 많은 동기화 관련 API 확인 필요
- **제한**: 문서화되지 않은 API 사용 위험

### 4.4 사용자 설정 중심 접근
- **현재 방식**: 사용자 설정 + 실제 상태 비교
- **개선 방안**: 사용자 설정만으로 상태 판단
- **장점**: 단순하고 안정적
- **단점**: 실제 하드웨어 상태 반영 안됨

## 5. 테스트 결과 기록

### 5.1 2025-06-25 테스트 결과

#### 5.1.1 기본 상태
```
displaySyncState: 1
isDisplayMasterable: True
count: 0
name: (빈 문자열)
uname: invalid
```

#### 5.1.2 사용자 설정: Slave
- **결과**: 설정 충돌 (빨간색)
- **이유**: 사용자 설정(Slave) ≠ 실제 상태(Master)
- **문제**: isDisplayMasterable이 항상 True

#### 5.1.3 사용자 설정: Master
- **예상 결과**: 동기화됨 (초록색)
- **실제 결과**: 확인 필요

### 5.2 다른 시스템에서의 테스트 필요
- **Master 시스템**: isDisplayMasterable 값 확인
- **Slave 시스템**: isDisplayMasterable 값 확인
- **동기화 꺼진 상태**: displaySyncState 값 확인

## 6. 다음 단계 계획

### 6.1 즉시 실행 가능한 작업
1. **isDisplayMasterable False 케이스 확인**
   - 다른 시스템에서 테스트
   - 동기화 꺼진 상태에서 테스트
   - 다른 NVIDIA 드라이버 버전에서 테스트

2. **ver 속성 상세 분석**
   - ManagementBaseObject 내부 속성 확인
   - 버전 정보에서 Master/Slave 정보 추출 가능성 검토

3. **다른 WMI 클래스 탐색**
   - NVIDIA 관련 다른 클래스 확인
   - 시스템 정보 관련 클래스 확인

### 6.2 중장기 계획
1. **레지스트리 기반 접근법 개발**
   - NVIDIA 제어판 설정 레지스트리 위치 파악
   - 설정 값 읽기 로직 구현

2. **NVAPI 확장 활용**
   - 더 많은 동기화 관련 API 확인
   - 문서화되지 않은 API 실험적 사용

3. **하이브리드 접근법**
   - WMI + 레지스트리 + NVAPI 조합
   - 각 소스의 장점 활용

## 7. 코드 구조 분석

### 7.1 핵심 클래스

#### 7.1.1 SyncChecker
- **위치**: `SyncGuard.Core/Class1.cs`
- **역할**: 동기화 상태 진단 및 모니터링
- **주요 메서드**:
  - `DiagnoseDisplaySync()`: WMI 기반 진단
  - `GetSyncStatus()`: 상태 반환
  - `SetUserRole()`: 사용자 역할 설정
  - `RefreshSyncStatus()`: 상태 새로고침

#### 7.1.2 NvApiWrapper
- **위치**: `SyncGuard.Core/NvApiWrapper.cs`
- **역할**: NVAPI 래핑
- **기능**: GPU 정보, 드라이버 정보 등

#### 7.1.3 Form1 (트레이 UI)
- **위치**: `SyncGuard.Tray/Form1.cs`
- **역할**: 트레이 아이콘 및 메뉴
- **기능**: 상태 표시, 설정 변경, 수동 새로고침

### 7.2 설정 관리
- **파일**: `syncguard_config.txt`
- **형식**: 단순 텍스트 (Master/Slave)
- **특징**: 프로그램 시작 시 자동 리셋

### 7.3 로깅 시스템
- **파일**: `syncguard_log.txt`
- **내용**: 모든 진단 과정 및 결과
- **용도**: 디버깅 및 문제 분석

## 8. 결론 및 권장사항

### 8.1 현재 상황
- `isDisplayMasterable` 속성이 신뢰할 수 없음
- 다른 WMI 속성들도 Master/Slave 판단에 부적합
- 사용자 설정 중심의 접근법이 더 안정적일 수 있음

### 8.2 권장사항
1. **즉시**: isDisplayMasterable False 케이스 확인
2. **단기**: ver 속성 상세 분석 및 다른 WMI 클래스 탐색
3. **중기**: 레지스트리 기반 접근법 개발
4. **장기**: 하이브리드 접근법으로 완성도 향상

### 8.3 성공 기준
- Master/Slave 설정이 정확히 반영됨
- 동기화 상태가 실제 하드웨어 상태와 일치
- 안정적이고 일관된 동작
- 사용자 친화적인 UI/UX

---

**문서 작성일**: 2025-06-25  
**작성자**: AI Assistant  
**버전**: 1.0  
**상태**: 진행 중 