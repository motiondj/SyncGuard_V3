@echo off
echo SyncGuard V3 Packaging Started...
echo.

REM Create build directories
if not exist "build" mkdir build
if not exist "build\logs" mkdir build\logs
if not exist "build\config" mkdir build\config

echo 1. Building projects...
dotnet build SyncGuard.Core --configuration Release

if %ERRORLEVEL% neq 0 (
    echo Core build failed!
    pause
    exit /b 1
)

dotnet build SyncGuard.Tray --configuration Release

if %ERRORLEVEL% neq 0 (
    echo Tray build failed!
    pause
    exit /b 1
)

echo 2. Copying executable files...
copy "SyncGuard.Tray\bin\Release\net9.0-windows\SyncGuard.Tray.exe" "build\"
copy "SyncGuard.Core\bin\Release\net9.0-windows\SyncGuard.Core.dll" "build\"

echo 3. Copying dependency files...
copy "SyncGuard.Tray\bin\Release\net9.0-windows\*.dll" "build\"

echo 4. Copying configuration files...
copy "SyncGuard.Installer\config\syncguard_config.txt" "build\config\"

echo 5. Copying documentation files...
copy "SyncGuard.Installer\README.txt" "build\"
copy "SyncGuard.Installer\LICENSE.txt" "build\"

echo 6. Creating portable package...
powershell -Command "Compress-Archive -Path 'build\*' -DestinationPath 'SyncGuard_V3_Portable.zip' -Force"

echo.
echo Packaging completed!
echo Generated files:
echo - SyncGuard_V3_Portable.zip (Portable version)
echo - build\ folder (Installation files)
echo.
echo To create MSI installer with WiX Toolset:
echo 1. Install WiX Toolset v3.11+
echo 2. Build SyncGuard.Installer project in Visual Studio
echo 3. Generate SyncGuard_V3_Setup.msi
echo.
pause 