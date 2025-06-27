# SyncGuard V3 패키징 가이드

## 개요
SyncGuard V3의 인스톨러 패키징 과정에서 발생한 문제들과 해결 방법을 정리한 문서입니다.

## 발생한 문제들

### 1. .NET Runtime 호환성 문제

#### 문제 상황
- 초기 프로젝트가 `net9.0-windows`로 설정되어 있었음
- 시스템에 .NET 6.0 Runtime만 설치되어 있었음
- 설치 후 실행 시 "You must install .Net desktop Runtime" 오류 발생

#### 해결 방법
1. **프로젝트 타겟 프레임워크 변경**
   ```xml
   <!-- SyncGuard.Tray.csproj -->
   <TargetFramework>net6.0-windows</TargetFramework>
   
   <!-- SyncGuard.Core.csproj -->
   <TargetFramework>net6.0-windows</TargetFramework>
   ```

2. **패키지 버전 호환성 업데이트**
   ```xml
   <PackageReference Include="System.Configuration.ConfigurationManager" Version="6.0.0" />
   <PackageReference Include="System.Diagnostics.EventLog" Version="6.0.0" />
   <PackageReference Include="System.Diagnostics.PerformanceCounter" Version="6.0.1" />
   <PackageReference Include="System.Management" Version="6.0.0" />
   <PackageReference Include="System.Security.Cryptography.ProtectedData" Version="6.0.0" />
   ```

### 2. Self-Contained 배포로 완전 해결

#### 문제 상황
- .NET Runtime 의존성으로 인한 설치 복잡성
- 사용자가 별도로 .NET Runtime을 설치해야 하는 불편함
- 재부팅 요구사항으로 인한 사용자 경험 저하

#### 해결 방법
1. **Self-Contained 설정 추가**
   ```xml
   <PropertyGroup>
     <PublishSingleFile>true</PublishSingleFile>
     <SelfContained>true</SelfContained>
     <RuntimeIdentifier>win-x64</RuntimeIdentifier>
   </PropertyGroup>
   ```

2. **단일 실행 파일 생성**
   ```powershell
   dotnet publish SyncGuard.Tray -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
   ```

## 최종 패키징 방법

### 1. Self-Contained 빌드
```powershell
# 프로젝트 빌드
dotnet build SyncGuard.Tray -c Release

# Self-contained 단일 파일 생성
dotnet publish SyncGuard.Tray -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 2. Inno Setup 스크립트 수정

#### 기존 방식 (문제가 있던 방식)
```ini
[Files]
; 메인 실행 파일
Source: "..\build\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\SyncGuard.Core.dll"; DestDir: "{app}"; Flags: ignoreversion

; 의존성 라이브러리들
Source: "..\build\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; .NET 6.0 런타임 오프라인 설치 파일 포함
Source: "windowsdesktop-runtime-6.0.36-win-x64.exe"; DestDir: "{tmp}"; Flags: ignoreversion
```

#### 개선된 방식 (Self-Contained)
```ini
[Files]
; Self-contained 단일 실행 파일만 포함
Source: "..\SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\publish\SyncGuard.Tray.exe"; DestDir: "{app}"; Flags: ignoreversion
; 설정 파일 (기본값)
Source: "config\syncguard_config.txt"; DestDir: "{app}\config"; Flags: ignoreversion
```

### 3. 제거된 불필요한 코드들

#### 제거된 .NET Runtime 체크 및 설치 코드
- `InitializeSetup()` 함수의 .NET Runtime 확인 로직
- `CurStepChanged()` 함수의 .NET Runtime 설치 로직
- 재부팅 안내 메시지 및 관련 코드
- `[Run]` 섹션의 재부팅 실행 코드

## 결과 및 개선사항

### 1. 파일 크기 최적화
- **기존**: 56.86 MB (여러 DLL + .NET Runtime 설치 파일 포함)
- **개선**: 42.83 MB (단일 실행 파일만 포함, 약 25% 감소)

### 2. 사용자 경험 개선
- ✅ .NET Runtime 설치 불필요
- ✅ 재부팅 불필요
- ✅ 설치 후 즉시 사용 가능
- ✅ 어떤 Windows PC에서도 바로 실행

### 3. 배포 간소화
- 단일 실행 파일로 모든 의존성 포함
- 복잡한 설치 과정 제거
- 사용자 실수 가능성 최소화

## 패키징 명령어

### 전체 패키징 프로세스
```powershell
# 1. 패키징 스크립트 실행
.\build_installer.ps1

# 2. 생성된 파일들
# - Output\SyncGuard_V3_Setup.exe (인스톨러)
# - SyncGuard_V3_Portable.zip (포터블 버전)
```

### 수동 빌드 (필요시)
```powershell
# 프로젝트 빌드
dotnet build SyncGuard.Tray -c Release

# Self-contained 배포
dotnet publish SyncGuard.Tray -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Inno Setup 컴파일
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "SyncGuard.Installer\SyncGuard_Setup.iss"
```

## 주의사항

### 1. Windows Forms 트리밍 제한
- Windows Forms 애플리케이션에서는 `PublishTrimmed=true` 사용 불가
- 트리밍 시 런타임 오류 발생 가능

### 2. 파일 크기 vs 호환성
- Self-contained 방식은 파일 크기가 커지지만 호환성이 뛰어남
- Framework-dependent 방식은 파일 크기가 작지만 .NET Runtime 필요

## 결론

Self-Contained 배포 방식으로 전환함으로써:
1. **사용자 편의성 대폭 향상**
2. **설치 과정 단순화**
3. **호환성 문제 완전 해결**
4. **유지보수성 향상**

이제 SyncGuard V3는 어떤 Windows 환경에서도 별도의 런타임 설치 없이 바로 실행할 수 있는 완전 독립형 애플리케이션이 되었습니다. 