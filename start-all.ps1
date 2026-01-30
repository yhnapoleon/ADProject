# EcoLens Start All - Backend + Frontend
# Run: .\start-all.ps1  or double-click in Explorer

$root = $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  EcoLens Dev Environment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

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
