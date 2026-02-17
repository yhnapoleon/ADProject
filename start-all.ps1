# EcoLens Start All - Backend + Frontend
# Run: .\start-all.ps1  or double-click in Explorer

$root = $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  EcoLens Dev Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 先结束占用 5133/5173 的旧进程，确保新启动的是最新代码
try {
    $conn5133 = Get-NetTCPConnection -LocalPort 5133 -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty OwningProcess
    $conn5173 = Get-NetTCPConnection -LocalPort 5173 -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty OwningProcess
    if ($conn5133) { Stop-Process -Id $conn5133 -Force -ErrorAction SilentlyContinue; Write-Host "[OK] Stopped process on 5133 (backend)" -ForegroundColor Yellow }
    if ($conn5173) { Stop-Process -Id $conn5173 -Force -ErrorAction SilentlyContinue; Write-Host "[OK] Stopped process on 5173 (frontend)" -ForegroundColor Yellow }
    if ($conn5133 -or $conn5173) { Start-Sleep -Seconds 1 }
} catch { }

# 重新编译后端（强制全量编译），确保使用最新代码
Write-Host "Building backend (no-incremental)..." -ForegroundColor Gray
Push-Location "$root\.NET\EcoLens.Api"
dotnet build --no-incremental -v q 2>&1 | Out-Null
Pop-Location
Start-Sleep -Seconds 1
Write-Host ""

# Check Vision service
try {
    $null = Invoke-WebRequest -Uri "http://localhost:8000/docs" -TimeoutSec 2 -UseBasicParsing -ErrorAction Stop
    Write-Host "[Vision] Running on port 8000" -ForegroundColor Green
} catch {
    Write-Host "[Vision] Not running - food recognition disabled" -ForegroundColor Yellow
    Write-Host "  Start: cd VisionService; .\.venv\Scripts\Activate.ps1; python main.py" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Opening Backend and Frontend windows..." -ForegroundColor Yellow

# Backend
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\.NET\EcoLens.Api'; Write-Host '=== Backend API :5133 ===' -ForegroundColor Green; dotnet run --launch-profile http"

Start-Sleep -Seconds 2

# Frontend
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\web'; Write-Host '=== Frontend :5173 ===' -ForegroundColor Green; npm run dev"

Write-Host ""
Write-Host "----------------------------------------" -ForegroundColor Cyan
Write-Host "  Frontend: http://localhost:5173" -ForegroundColor White
Write-Host "  Backend:  http://localhost:5133" -ForegroundColor White
Write-Host "----------------------------------------" -ForegroundColor Cyan
Write-Host "Wait ~10 sec then open http://localhost:5173 in browser" -ForegroundColor Gray
Write-Host "Close each window to stop that service." -ForegroundColor Gray
Write-Host ""
