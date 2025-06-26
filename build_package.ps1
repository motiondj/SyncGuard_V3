# SyncGuard V3 Packaging Script
Write-Host "SyncGuard V3 Packaging Started..." -ForegroundColor Green
Write-Host ""

# Create build directories
$buildDir = "build"
$logsDir = "$buildDir\logs"
$configDir = "$buildDir\config"

if (!(Test-Path $buildDir)) { New-Item -ItemType Directory -Path $buildDir }
if (!(Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir }
if (!(Test-Path $configDir)) { New-Item -ItemType Directory -Path $configDir }

Write-Host "1. Building projects..." -ForegroundColor Yellow
dotnet build SyncGuard.Core --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Core build failed!" -ForegroundColor Red
    Read-Host "Press any key to continue"
    exit 1
}

dotnet build SyncGuard.Tray --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tray build failed!" -ForegroundColor Red
    Read-Host "Press any key to continue"
    exit 1
}

Write-Host "2. Copying executable files..." -ForegroundColor Yellow
$trayBinDir = "SyncGuard.Tray\bin\Release\net9.0-windows"
$coreBinDir = "SyncGuard.Core\bin\Release\net9.0-windows"

if (Test-Path $trayBinDir) {
    Copy-Item "$trayBinDir\SyncGuard.Tray.exe" $buildDir -Force
    Copy-Item "$trayBinDir\*.dll" $buildDir -Force
    Write-Host "   - SyncGuard.Tray.exe copied" -ForegroundColor Gray
}

if (Test-Path $coreBinDir) {
    Copy-Item "$coreBinDir\SyncGuard.Core.dll" $buildDir -Force
    Write-Host "   - SyncGuard.Core.dll copied" -ForegroundColor Gray
}

Write-Host "3. Copying configuration files..." -ForegroundColor Yellow
if (Test-Path "SyncGuard.Installer\config\syncguard_config.txt") {
    Copy-Item "SyncGuard.Installer\config\syncguard_config.txt" $configDir -Force
    Write-Host "   - syncguard_config.txt copied" -ForegroundColor Gray
}

Write-Host "4. Copying documentation files..." -ForegroundColor Yellow
if (Test-Path "SyncGuard.Installer\README.txt") {
    Copy-Item "SyncGuard.Installer\README.txt" $buildDir -Force
    Write-Host "   - README.txt copied" -ForegroundColor Gray
}
if (Test-Path "SyncGuard.Installer\LICENSE.txt") {
    Copy-Item "SyncGuard.Installer\LICENSE.txt" $buildDir -Force
    Write-Host "   - LICENSE.txt copied" -ForegroundColor Gray
}

Write-Host "5. Creating portable package..." -ForegroundColor Yellow
$portableZip = "SyncGuard_V3_Portable.zip"
if (Test-Path $portableZip) { Remove-Item $portableZip -Force }

Compress-Archive -Path "$buildDir\*" -DestinationPath $portableZip -Force

Write-Host ""
Write-Host "Packaging completed!" -ForegroundColor Green
Write-Host "Generated files:" -ForegroundColor Cyan
Write-Host "- $portableZip (Portable version)" -ForegroundColor White
Write-Host "- $buildDir folder (Installation files)" -ForegroundColor White
Write-Host ""
Write-Host "To create MSI installer with WiX Toolset:" -ForegroundColor Yellow
Write-Host "1. Install WiX Toolset v3.11+" -ForegroundColor White
Write-Host "2. Build SyncGuard.Installer project in Visual Studio" -ForegroundColor White
Write-Host "3. Generate SyncGuard_V3_Setup.msi" -ForegroundColor White
Write-Host ""

# Display file size information
if (Test-Path $portableZip) {
    $size = (Get-Item $portableZip).Length
    $sizeMB = [math]::Round($size / 1MB, 2)
    Write-Host "Portable package size: $sizeMB MB" -ForegroundColor Cyan
}

# List files in build directory
Write-Host ""
Write-Host "Files in build directory:" -ForegroundColor Cyan
Get-ChildItem $buildDir -Recurse | ForEach-Object {
    $relativePath = $_.FullName.Replace((Get-Location).Path + "\", "")
    Write-Host "  $relativePath" -ForegroundColor Gray
}

Read-Host "Press any key to continue" 