# SyncGuard Monitor Packaging Script
Write-Host "SyncGuard Monitor Packaging Started..." -ForegroundColor Green
Write-Host ""

# Create build directories
$buildDir = "build_monitor"
$logsDir = "$buildDir\logs"
$configDir = "$buildDir\config"

if (!(Test-Path $buildDir)) { New-Item -ItemType Directory -Path $buildDir }
if (!(Test-Path $logsDir)) { New-Item -ItemType Directory -Path $logsDir }
if (!(Test-Path $configDir)) { New-Item -ItemType Directory -Path $configDir }

Write-Host "1. Building SyncGuard Monitor project..." -ForegroundColor Yellow
dotnet build SyncGuardMonitor --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "SyncGuard Monitor build failed!" -ForegroundColor Red
    Read-Host "Press any key to continue"
    exit 1
}

Write-Host "2. Publishing SyncGuard Monitor..." -ForegroundColor Yellow
dotnet publish SyncGuardMonitor --configuration Release --output "$buildDir\publish" --self-contained true --runtime win-x64
if ($LASTEXITCODE -ne 0) {
    Write-Host "SyncGuard Monitor publish failed!" -ForegroundColor Red
    Read-Host "Press any key to continue"
    exit 1
}

Write-Host "3. Copying executable files..." -ForegroundColor Yellow
$monitorPublishDir = "$buildDir\publish"

if (Test-Path $monitorPublishDir) {
    Copy-Item "$monitorPublishDir\SyncGuardMonitor.exe" $buildDir -Force
    Copy-Item "$monitorPublishDir\*.dll" $buildDir -Force
    Write-Host "   - SyncGuardMonitor.exe copied" -ForegroundColor Gray
    Write-Host "   - Runtime DLLs copied" -ForegroundColor Gray
}

Write-Host "4. Creating configuration files..." -ForegroundColor Yellow
# 기본 설정 파일 생성
$configContent = @"
# SyncGuard Monitor Configuration
[Monitor]
Port=8888
MaxConnections=100
LogLevel=Information

[UI]
AutoRefresh=5000
ShowNotifications=true
"@

$configContent | Out-File -FilePath "$configDir\monitor_config.txt" -Encoding UTF8
Write-Host "   - monitor_config.txt created" -ForegroundColor Gray

Write-Host "5. Creating documentation files..." -ForegroundColor Yellow
# README 파일 생성
$readmeContent = @"
SyncGuard Monitor v1.0.0
========================

TCP 기반 SyncGuard 상태 모니터링 독립 소프트웨어

사용법:
1. SyncGuardMonitor.exe를 실행합니다.
2. 기본 포트 8888에서 모니터링을 시작합니다.
3. SyncGuard 클라이언트들이 연결되면 상태를 확인할 수 있습니다.

설정:
- config/monitor_config.txt 파일에서 포트 및 기타 설정을 변경할 수 있습니다.

시스템 요구사항:
- Windows 10/11 (x64)
- .NET 6.0 Runtime (포함됨)

라이선스:
Copyright © 2025 SyncGuard Monitor Team
"@

$readmeContent | Out-File -FilePath "$buildDir\README.txt" -Encoding UTF8
Write-Host "   - README.txt created" -ForegroundColor Gray

# LICENSE 파일 생성
$licenseContent = @"
MIT License

Copyright (c) 2025 SyncGuard Monitor Team

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
"@

$licenseContent | Out-File -FilePath "$buildDir\LICENSE.txt" -Encoding UTF8
Write-Host "   - LICENSE.txt created" -ForegroundColor Gray

Write-Host "6. Creating portable package..." -ForegroundColor Yellow
$portableZip = "SyncGuardMonitor_V1.0.0_Portable.zip"
if (Test-Path $portableZip) { Remove-Item $portableZip -Force }

Compress-Archive -Path "$buildDir\*" -DestinationPath $portableZip -Force

Write-Host ""
Write-Host "Packaging completed!" -ForegroundColor Green
Write-Host "Generated files:" -ForegroundColor Cyan
Write-Host "- $portableZip (Portable version)" -ForegroundColor White
Write-Host "- $buildDir folder (Installation files)" -ForegroundColor White
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

Write-Host ""
Write-Host "SyncGuard Monitor 패키징이 완료되었습니다!" -ForegroundColor Green
Write-Host "사용 가능한 파일:" -ForegroundColor Cyan
Write-Host "- $portableZip (포터블 버전)" -ForegroundColor White
Write-Host "- $buildDir 폴더 (설치 파일들)" -ForegroundColor White

Read-Host "Press any key to continue" 