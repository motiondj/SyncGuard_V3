@echo off
chcp 65001 >nul
echo ========================================
echo SyncGuard V3 포터블 버전 생성
echo ========================================

set "SOURCE_DIR=SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\publish"
set "OUTPUT_DIR=Output\SyncGuard_V3_Portable"
set "CONFIG_DIR=SyncGuard.Installer\config"

echo.
echo 1. 출력 디렉토리 정리...
if exist "%OUTPUT_DIR%" rmdir /s /q "%OUTPUT_DIR%"
mkdir "%OUTPUT_DIR%"

echo.
echo 2. 실행 파일 복사...
copy "%SOURCE_DIR%\SyncGuard.Tray.exe" "%OUTPUT_DIR%\"
copy "%SOURCE_DIR%\*.dll" "%OUTPUT_DIR%\"

echo.
echo 3. 설정 파일 복사...
if exist "%CONFIG_DIR%\default_config.json" (
    copy "%CONFIG_DIR%\default_config.json" "%OUTPUT_DIR%\syncguard_config.json"
) else (
    echo 기본 설정 파일을 생성합니다...
    echo { > "%OUTPUT_DIR%\syncguard_config.json"
    echo   "serverIP": "127.0.0.1", >> "%OUTPUT_DIR%\syncguard_config.json"
    echo   "serverPort": 8080, >> "%OUTPUT_DIR%\syncguard_config.json"
    echo   "transmissionInterval": 1000, >> "%OUTPUT_DIR%\syncguard_config.json"
    echo   "enableExternalSend": false >> "%OUTPUT_DIR%\syncguard_config.json"
    echo } >> "%OUTPUT_DIR%\syncguard_config.json"
)

echo.
echo 4. README 파일 생성...
echo SyncGuard V3 포터블 버전 > "%OUTPUT_DIR%\README.txt"
echo. >> "%OUTPUT_DIR%\README.txt"
echo 사용법: >> "%OUTPUT_DIR%\README.txt"
echo 1. SyncGuard.Tray.exe를 실행하세요. >> "%OUTPUT_DIR%\README.txt"
echo 2. 트레이 아이콘에서 설정을 변경할 수 있습니다. >> "%OUTPUT_DIR%\README.txt"
echo 3. 로그는 %LOCALAPPDATA%\SyncGuard\logs 폴더에 저장됩니다. >> "%OUTPUT_DIR%\README.txt"
echo. >> "%OUTPUT_DIR%\README.txt"
echo 시스템 요구사항: >> "%OUTPUT_DIR%\README.txt"
echo - Windows 10/11 (x64) >> "%OUTPUT_DIR%\README.txt"
echo - NVIDIA Quadro GPU + Quadro Sync 카드 (동기화 기능 사용 시) >> "%OUTPUT_DIR%\README.txt"
echo. >> "%OUTPUT_DIR%\README.txt"
echo 버전: 3.0.0 >> "%OUTPUT_DIR%\README.txt"
echo 빌드 날짜: %date% %time% >> "%OUTPUT_DIR%\README.txt"

echo.
echo 5. 실행 스크립트 생성...
echo @echo off > "%OUTPUT_DIR%\SyncGuard.bat"
echo cd /d "%%~dp0" >> "%OUTPUT_DIR%\SyncGuard.bat"
echo start "" "SyncGuard.Tray.exe" >> "%OUTPUT_DIR%\SyncGuard.bat"

echo.
echo ========================================
echo 포터블 버전 생성 완료!
echo 위치: %OUTPUT_DIR%
echo ========================================
echo.
pause 