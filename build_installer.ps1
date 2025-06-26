# SyncGuard V3 Installer Build Script
Write-Host "SyncGuard V3 Installer Build Started..." -ForegroundColor Green
Write-Host ""

# Inno Setup 경로 확인
$innoCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

if (!(Test-Path $innoCompiler)) {
    Write-Host "Inno Setup 6이 설치되어 있지 않습니다." -ForegroundColor Red
    Write-Host "https://jrsoftware.org/isdl.php 에서 다운로드하여 설치해주세요." -ForegroundColor Yellow
    Read-Host "Press any key to continue"
    exit 1
}

# Output 폴더 생성
$outputDir = "Output"
if (!(Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir }

Write-Host "1. Building portable package first..." -ForegroundColor Yellow
& ".\build_package.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Portable package build failed!" -ForegroundColor Red
    Read-Host "Press any key to continue"
    exit 1
}

Write-Host "2. Creating installer with Inno Setup..." -ForegroundColor Yellow
& $innoCompiler "SyncGuard.Installer\SyncGuard_Setup.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Installer creation failed!" -ForegroundColor Red
    Read-Host "Press any key to continue"
    exit 1
}

Write-Host ""
Write-Host "Installer build completed!" -ForegroundColor Green
Write-Host "Generated files:" -ForegroundColor Cyan
Write-Host "- $outputDir\SyncGuard_V3_Setup.exe (Installer)" -ForegroundColor White
Write-Host "- SyncGuard_V3_Portable.zip (Portable version)" -ForegroundColor White
Write-Host ""
Write-Host "Installer features:" -ForegroundColor Yellow
Write-Host "- Modern wizard interface" -ForegroundColor White
Write-Host "- Korean language support" -ForegroundColor White
Write-Host "- Desktop shortcut option" -ForegroundColor White
Write-Host "- Auto-start option" -ForegroundColor White
Write-Host "- Proper uninstall support" -ForegroundColor White
Write-Host "- .NET Runtime check" -ForegroundColor White
Write-Host ""

# 파일 크기 정보 표시
$installerPath = "$outputDir\SyncGuard_V3_Setup.exe"
if (Test-Path $installerPath) {
    $size = (Get-Item $installerPath).Length
    $sizeMB = [math]::Round($size / 1MB, 2)
    Write-Host "Installer size: $sizeMB MB" -ForegroundColor Cyan
}

Read-Host "Press any key to continue" 