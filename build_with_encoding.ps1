# SyncGuard V3 빌드 스크립트 (UTF-8 인코딩)
# PowerShell에서 한글 출력을 위한 인코딩 설정
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SyncGuard V3 빌드 스크립트 (UTF-8 인코딩)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. 프로젝트 빌드
Write-Host "[1/4] 프로젝트 빌드 중..." -ForegroundColor Yellow
dotnet build -c Release | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 빌드 실패" -ForegroundColor Red
    Read-Host "계속하려면 아무 키나 누르세요"
    exit 1
}
Write-Host "✅ 빌드 완료" -ForegroundColor Green
Write-Host ""

# 2. Self-contained 빌드
Write-Host "[2/4] Self-contained 빌드 중..." -ForegroundColor Yellow
dotnet publish SyncGuard.Tray -c Release -r win-x64 --self-contained true -o "SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\self-contained" | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Self-contained 빌드 실패" -ForegroundColor Red
    Read-Host "계속하려면 아무 키나 누르세요"
    exit 1
}
Write-Host "✅ Self-contained 빌드 완료" -ForegroundColor Green
Write-Host ""

# 3. 설치 파일 생성
Write-Host "[3/4] 설치 파일 생성 중..." -ForegroundColor Yellow
Push-Location SyncGuard.Installer
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" SyncGuard_Setup.iss | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 설치 파일 생성 실패" -ForegroundColor Red
    Pop-Location
    Read-Host "계속하려면 아무 키나 누르세요"
    exit 1
}
Pop-Location
Write-Host "✅ 설치 파일 생성 완료" -ForegroundColor Green
Write-Host ""

# 4. 포터블 버전 생성
Write-Host "[4/4] 포터블 버전 생성 중..." -ForegroundColor Yellow
$portableDir = "Output\SyncGuard_V3_Portable"
$sourceDir = "SyncGuard.Tray\bin\Release\net6.0-windows\win-x64\self-contained"

if (!(Test-Path $portableDir)) {
    New-Item -ItemType Directory -Path $portableDir -Force | Out-Null
}

Copy-Item -Path "$sourceDir\*" -Destination $portableDir -Recurse -Force
Write-Host "✅ 포터블 버전 생성 완료" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "🎉 모든 빌드가 완료되었습니다!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📁 설치 파일: Output\SyncGuard_V3_Setup.exe" -ForegroundColor White
Write-Host "📁 포터블 버전: Output\SyncGuard_V3_Portable\" -ForegroundColor White
Write-Host ""

Read-Host "계속하려면 아무 키나 누르세요" 