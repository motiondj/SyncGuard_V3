@echo off
chcp 65001 >nul
echo ========================================
echo SyncGuard V3 빌드 스크립트 (UTF-8 인코딩)
echo ========================================
echo.

echo [1/4] 프로젝트 빌드 중...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ❌ 빌드 실패
    pause
    exit /b 1
)
echo ✅ 빌드 완료
echo.

echo [2/4] Self-contained 빌드 중...
dotnet publish SyncGuard.Tray -c Release -r win-x64 --self-contained true -o SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\self-contained
if %errorlevel% neq 0 (
    echo ❌ Self-contained 빌드 실패
    pause
    exit /b 1
)
echo ✅ Self-contained 빌드 완료
echo.

echo [3/4] 설치 파일 생성 중...
cd SyncGuard.Installer
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" SyncGuard_Setup.iss
if %errorlevel% neq 0 (
    echo ❌ 설치 파일 생성 실패
    cd ..
    pause
    exit /b 1
)
cd ..
echo ✅ 설치 파일 생성 완료
echo.

echo [4/4] 포터블 버전 생성 중...
if not exist "Output\SyncGuard_V3_Portable" mkdir "Output\SyncGuard_V3_Portable"
xcopy "SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\self-contained\*" "Output\SyncGuard_V3_Portable\" /E /Y /Q
echo ✅ 포터블 버전 생성 완료
echo.

echo ========================================
echo 🎉 모든 빌드가 완료되었습니다!
echo ========================================
echo.
echo 📁 설치 파일: Output\SyncGuard_V3_Setup.exe
echo 📁 포터블 버전: Output\SyncGuard_V3_Portable\
echo.
pause 