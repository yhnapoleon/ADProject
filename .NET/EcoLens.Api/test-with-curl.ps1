# Test Upload using real curl.exe
param(
    [string]$FilePath = "E:\OneDrive\Desktop\AD\Test.jpg",
    [string]$BaseUrl = "http://localhost:5133"
)

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Upload Test using curl.exe" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Login
Write-Host "`n=== Step 1: Login ===" -ForegroundColor Cyan
$timestamp = Get-Date -Format 'HHmmss'
$email = "test$timestamp@example.com"

$registerBody = @{
    username = "testuser$timestamp"
    email = $email
    password = "Test123!"
} | ConvertTo-Json

try {
    $null = Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"
    Write-Host "✓ Registered" -ForegroundColor Green
} catch {
    Write-Host "⚠ Registration skipped" -ForegroundColor Yellow
}

$loginBody = @{
    email = $email
    password = "Test123!"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
$token = $loginResponse.token
Write-Host "✓ Logged in" -ForegroundColor Green

# Step 2: Upload using curl.exe
Write-Host "`n=== Step 2: Upload File ===" -ForegroundColor Cyan
Write-Host "File: $FilePath" -ForegroundColor Gray
Write-Host "Uploading..." -ForegroundColor Yellow

# Use the real curl.exe from Windows
$curlPath = "C:\Windows\System32\curl.exe"
$curlCommand = "& `"$curlPath`" -X POST `"$BaseUrl/api/UtilityBill/upload`" -H `"Authorization: Bearer $token`" -F `"file=@$FilePath`""

try {
    $response = Invoke-Expression $curlCommand
    
    if ($LASTEXITCODE -eq 0) {
        $json = $response | ConvertFrom-Json
        
        Write-Host "✓ Upload successful!" -ForegroundColor Green
        Write-Host "`n=== Extracted Data ===" -ForegroundColor Cyan
        Write-Host "Bill ID: $($json.id)" -ForegroundColor White
        Write-Host "Bill Type: $($json.billTypeName)" -ForegroundColor White
        Write-Host "Period: $($json.billPeriodStart) to $($json.billPeriodEnd)" -ForegroundColor White
        Write-Host "Input Method: $($json.inputMethodName)" -ForegroundColor White
        if ($json.ocrConfidence) {
            Write-Host "OCR Confidence: $([math]::Round($json.ocrConfidence * 100, 2))%" -ForegroundColor White
        }
        
        Write-Host "`n=== Usage Data ===" -ForegroundColor Cyan
        if ($json.electricityUsage) {
            Write-Host "Electricity: $($json.electricityUsage) kWh" -ForegroundColor White
        }
        if ($json.waterUsage) {
            Write-Host "Water: $($json.waterUsage) m³" -ForegroundColor White
        }
        if ($json.gasUsage) {
            Write-Host "Gas: $($json.gasUsage)" -ForegroundColor White
        }
        
        Write-Host "`n=== Carbon Emissions ===" -ForegroundColor Cyan
        Write-Host "Electricity Carbon: $($json.electricityCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Water Carbon: $($json.waterCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Gas Carbon: $($json.gasCarbonEmission) kg CO2" -ForegroundColor White
        Write-Host "Total Carbon: $($json.totalCarbonEmission) kg CO2" -ForegroundColor Green
        
        Write-Host "`n=== Test Complete ===" -ForegroundColor Green
    } else {
        Write-Host "✗ Upload failed (exit code: $LASTEXITCODE)" -ForegroundColor Red
        Write-Host "Response: $response" -ForegroundColor Yellow
    }
} catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
}
