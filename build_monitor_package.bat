@echo off
chcp 65001 >nul
echo SyncGuard Monitor 패키징을 시작합니다...
echo.

REM 빌드 디렉토리 생성
set buildDir=build_monitor
set logsDir=%buildDir%\logs
set configDir=%buildDir%\config

if not exist %buildDir% mkdir %buildDir%
if not exist %logsDir% mkdir %logsDir%
if not exist %configDir% mkdir %configDir%

echo 1. SyncGuard Monitor 프로젝트 빌드 중...
dotnet build SyncGuardMonitor --configuration Release
if %ERRORLEVEL% neq 0 (
    echo SyncGuard Monitor 빌드 실패!
    pause
    exit /b 1
)

echo 2. SyncGuard Monitor 게시 중...
dotnet publish SyncGuardMonitor --configuration Release --output "%buildDir%\publish" --self-contained true --runtime win-x64
if %ERRORLEVEL% neq 0 (
    echo SyncGuard Monitor 게시 실패!
    pause
    exit /b 1
)

echo 3. 실행 파일 복사 중...
set monitorPublishDir=%buildDir%\publish

if exist %monitorPublishDir% (
    copy "%monitorPublishDir%\SyncGuardMonitor.exe" %buildDir% >nul
    copy "%monitorPublishDir%\*.dll" %buildDir% >nul
    echo    - SyncGuardMonitor.exe 복사됨
    echo    - Runtime DLLs 복사됨
)

echo 4. 설정 파일 생성 중...
echo # SyncGuard Monitor Configuration > "%configDir%\monitor_config.txt"
echo [Monitor] >> "%configDir%\monitor_config.txt"
echo Port=8888 >> "%configDir%\monitor_config.txt"
echo MaxConnections=100 >> "%configDir%\monitor_config.txt"
echo LogLevel=Information >> "%configDir%\monitor_config.txt"
echo. >> "%configDir%\monitor_config.txt"
echo [UI] >> "%configDir%\monitor_config.txt"
echo AutoRefresh=5000 >> "%configDir%\monitor_config.txt"
echo ShowNotifications=true >> "%configDir%\monitor_config.txt"
echo    - monitor_config.txt 생성됨

echo 5. 문서 파일 생성 중...
echo SyncGuard Monitor v1.0.0 > "%buildDir%\README.txt"
echo ======================== >> "%buildDir%\README.txt"
echo. >> "%buildDir%\README.txt"
echo TCP 기반 SyncGuard 상태 모니터링 독립 소프트웨어 >> "%buildDir%\README.txt"
echo. >> "%buildDir%\README.txt"
echo 사용법: >> "%buildDir%\README.txt"
echo 1. SyncGuardMonitor.exe를 실행합니다. >> "%buildDir%\README.txt"
echo 2. 기본 포트 8888에서 모니터링을 시작합니다. >> "%buildDir%\README.txt"
echo 3. SyncGuard 클라이언트들이 연결되면 상태를 확인할 수 있습니다. >> "%buildDir%\README.txt"
echo. >> "%buildDir%\README.txt"
echo 설정: >> "%buildDir%\README.txt"
echo - config/monitor_config.txt 파일에서 포트 및 기타 설정을 변경할 수 있습니다. >> "%buildDir%\README.txt"
echo. >> "%buildDir%\README.txt"
echo 시스템 요구사항: >> "%buildDir%\README.txt"
echo - Windows 10/11 (x64) >> "%buildDir%\README.txt"
echo - .NET 6.0 Runtime (포함됨) >> "%buildDir%\README.txt"
echo. >> "%buildDir%\README.txt"
echo 라이선스: >> "%buildDir%\README.txt"
echo Copyright © 2025 SyncGuard Monitor Team >> "%buildDir%\README.txt"
echo    - README.txt 생성됨

echo MIT License > "%buildDir%\LICENSE.txt"
echo. >> "%buildDir%\LICENSE.txt"
echo Copyright (c) 2025 SyncGuard Monitor Team >> "%buildDir%\LICENSE.txt"
echo. >> "%buildDir%\LICENSE.txt"
echo Permission is hereby granted, free of charge, to any person obtaining a copy >> "%buildDir%\LICENSE.txt"
echo of this software and associated documentation files (the "Software"), to deal >> "%buildDir%\LICENSE.txt"
echo in the Software without restriction, including without limitation the rights >> "%buildDir%\LICENSE.txt"
echo to use, copy, modify, merge, publish, distribute, sublicense, and/or sell >> "%buildDir%\LICENSE.txt"
echo copies of the Software, and to permit persons to whom the Software is >> "%buildDir%\LICENSE.txt"
echo furnished to do so, subject to the following conditions: >> "%buildDir%\LICENSE.txt"
echo. >> "%buildDir%\LICENSE.txt"
echo The above copyright notice and this permission notice shall be included in all >> "%buildDir%\LICENSE.txt"
echo copies or substantial portions of the Software. >> "%buildDir%\LICENSE.txt"
echo. >> "%buildDir%\LICENSE.txt"
echo THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR >> "%buildDir%\LICENSE.txt"
echo IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, >> "%buildDir%\LICENSE.txt"
echo FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE >> "%buildDir%\LICENSE.txt"
echo AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER >> "%buildDir%\LICENSE.txt"
echo LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, >> "%buildDir%\LICENSE.txt"
echo OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE >> "%buildDir%\LICENSE.txt"
echo SOFTWARE. >> "%buildDir%\LICENSE.txt"
echo    - LICENSE.txt 생성됨

echo 6. 포터블 패키지 생성 중...
set portableZip=SyncGuardMonitor_V1.0.0_Portable.zip
if exist %portableZip% del %portableZip%

powershell -Command "Compress-Archive -Path '%buildDir%\*' -DestinationPath '%portableZip%' -Force"

echo.
echo 패키징 완료!
echo 생성된 파일:
echo - %portableZip% (포터블 버전)
echo - %buildDir% 폴더 (설치 파일들)

if exist %portableZip% (
    for %%A in (%portableZip%) do set size=%%~zA
    set /a sizeMB=%size%/1048576
    echo 포터블 패키지 크기: %sizeMB% MB
)

echo.
echo %buildDir% 폴더의 파일들:
dir /s /b %buildDir%

echo.
echo SyncGuard Monitor 패키징이 완료되었습니다!
echo 사용 가능한 파일:
echo - %portableZip% (포터블 버전)
echo - %buildDir% 폴더 (설치 파일들)

pause 