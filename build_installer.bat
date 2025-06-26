@echo off
echo SyncGuard V3 Installer Build Started...
echo.

REM Inno Setup 경로 확인
set INNO_COMPILER="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if not exist %INNO_COMPILER% (
    echo Inno Setup 6이 설치되어 있지 않습니다.
    echo https://jrsoftware.org/isdl.php 에서 다운로드하여 설치해주세요.
    pause
    exit /b 1
)

REM Output 폴더 생성
if not exist "Output" mkdir Output

echo 1. Building portable package first...
call build_package.bat

if %ERRORLEVEL% neq 0 (
    echo Portable package build failed!
    pause
    exit /b 1
)

echo 2. Creating installer with Inno Setup...
%INNO_COMPILER% "SyncGuard.Installer\SyncGuard_Setup.iss"

if %ERRORLEVEL% neq 0 (
    echo Installer creation failed!
    pause
    exit /b 1
)

echo.
echo Installer build completed!
echo Generated files:
echo - Output\SyncGuard_V3_Setup.exe (Installer)
echo - SyncGuard_V3_Portable.zip (Portable version)
echo.
echo Installer features:
echo - Modern wizard interface
echo - Korean language support
echo - Desktop shortcut option
echo - Auto-start option
echo - Proper uninstall support
echo - .NET Runtime check
echo.
pause 